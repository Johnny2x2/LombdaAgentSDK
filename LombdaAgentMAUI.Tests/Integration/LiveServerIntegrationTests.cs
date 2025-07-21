using System.Net;
using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LombdaAgentMAUI.Tests.Integration
{
    /// <summary>
    /// Live integration tests that run against a real LombdaAgent API server.
    /// These tests require the API to be running and accessible.
    /// </summary>
    [TestFixture]
    [Category(TestCategories.Integration)]
    [Category(TestCategories.Network)]
    [RequiresNetwork("These tests require a running LombdaAgent API server")]
    public class LiveServerIntegrationTests
    {
        private ServiceProvider _serviceProvider;
        private IAgentApiService _apiService;
        private IConfigurationService _configService;
        
        // Configuration - Update these values to match your running API server
        private const string DEFAULT_API_URL = "http://localhost:5000/";
        private const string ALTERNATIVE_API_URL = "http://localhost:5000/"; // HTTP fallback
        
        // Test data
        private readonly List<string> _createdAgentIds = new();
        private string? _testAgentId;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Setup services for live testing
            var services = new ServiceCollection();
            
            // Use a test configuration service that doesn't persist to real storage
            services.AddSingleton<ISecureStorageService>(provider => new TestSecureStorageService());
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Use real HTTP client for live testing
            services.AddHttpClient<IAgentApiService, AgentApiService>(client =>
            {
                client.BaseAddress = new Uri(DEFAULT_API_URL);
                client.Timeout = TimeSpan.FromMinutes(2); // Longer timeout for real requests
            });
            
            _serviceProvider = services.BuildServiceProvider();
            _apiService = _serviceProvider.GetRequiredService<IAgentApiService>();
            _configService = _serviceProvider.GetRequiredService<IConfigurationService>();
            
            // Configure the API URL
            _configService.ApiBaseUrl = DEFAULT_API_URL;
            
            // Verify the server is accessible
            await VerifyServerAccessibility();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            // Clean up any agents created during testing
            await CleanupCreatedAgents();
            _serviceProvider?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            // Reset test state
            _testAgentId = null;
        }

        [TearDown]
        public async Task TearDown()
        {
            // Clean up any agents created in individual tests
            if (!string.IsNullOrEmpty(_testAgentId) && !_createdAgentIds.Contains(_testAgentId))
            {
                _createdAgentIds.Add(_testAgentId);
            }
        }

        private async Task VerifyServerAccessibility()
        {
            try
            {
                var agents = await _apiService.GetAgentsAsync();
                Console.WriteLine($"✅ Server is accessible. Found {agents.Count} existing agents.");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("HTTPS"))
            {
                // Try fallback to HTTP if HTTPS fails
                Console.WriteLine("⚠️ HTTPS connection failed, trying HTTP fallback...");
                _configService.ApiBaseUrl = ALTERNATIVE_API_URL;
                
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
                httpClient.BaseAddress = new Uri(ALTERNATIVE_API_URL);
                
                var agents = await _apiService.GetAgentsAsync();
                Console.WriteLine($"✅ HTTP fallback successful. Found {agents.Count} existing agents.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"❌ Cannot connect to LombdaAgent API server at {_configService.ApiBaseUrl}. " +
                           $"Please ensure the server is running.\nError: {ex.Message}\n\n" +
                           $"To start the server, run: dotnet run --project LombdaAgentAPI");
            }
        }

        [Test]
        [Category(TestCategories.Api)]
        public async Task LiveServer_GetAgents_ReturnsAgentList()
        {
            // Act
            var agents = await _apiService.GetAgentsAsync();

            // Assert
            Assert.That(agents, Is.Not.Null, "Agents list should not be null");
            Assert.That(agents, Is.InstanceOf<List<string>>(), "Should return a list of strings");
            
            Console.WriteLine($"📋 Found {agents.Count} agents on the server");
            foreach (var agent in agents.Take(5)) // Show first 5 agents
            {
                Console.WriteLine($"   - {agent}");
            }
            if (agents.Count > 5)
            {
                Console.WriteLine($"   ... and {agents.Count - 5} more");
            }
        }

        [Test]
        [Category(TestCategories.Api)]
        public async Task LiveServer_CreateAgent_ReturnsValidAgent()
        {
            // Arrange
            var agentName = $"TestAgent_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Act
            var createdAgent = await _apiService.CreateAgentAsync(agentName);

            // Assert
            Assert.That(createdAgent, Is.Not.Null, "Created agent should not be null");
            Assert.That(createdAgent.Id, Is.Not.Null.And.Not.Empty, "Agent ID should be provided");
            Assert.That(createdAgent.Name, Is.EqualTo(agentName), "Agent name should match requested name");

            // Store for cleanup
            _testAgentId = createdAgent.Id;
            
            Console.WriteLine($"✅ Created agent: {createdAgent.Name} (ID: {createdAgent.Id})");
        }

        [Test]
        [Category(TestCategories.Api)]
        public async Task LiveServer_GetAgent_ReturnsCorrectAgent()
        {
            // Arrange - Create an agent first
            var agentName = $"GetTestAgent_{DateTime.Now:yyyyMMdd_HHmmss}";
            var createdAgent = await _apiService.CreateAgentAsync(agentName);
            _testAgentId = createdAgent!.Id;

            // Act
            var retrievedAgent = await _apiService.GetAgentAsync(createdAgent.Id);

            // Assert
            Assert.That(retrievedAgent, Is.Not.Null, "Retrieved agent should not be null");
            Assert.That(retrievedAgent.Id, Is.EqualTo(createdAgent.Id), "Agent IDs should match");
            Assert.That(retrievedAgent.Name, Is.EqualTo(createdAgent.Name), "Agent names should match");

            Console.WriteLine($"✅ Retrieved agent: {retrievedAgent.Name} (ID: {retrievedAgent.Id})");
        }

        [Test]
        [Category(TestCategories.Api)]
        public async Task LiveServer_GetNonExistentAgent_ReturnsNull()
        {
            // Arrange
            var nonExistentId = "non-existent-agent-id-12345";

            // Act
            var result = await _apiService.GetAgentAsync(nonExistentId);

            // Assert
            Assert.That(result, Is.Null, "Non-existent agent should return null");

            Console.WriteLine($"✅ Correctly returned null for non-existent agent ID: {nonExistentId}");
        }

        [Test]
        [Category(TestCategories.Api)]
        [SlowTest("This test involves actual AI agent processing which can take time")]
        public async Task LiveServer_SendMessage_ReturnsValidResponse()
        {
            // Arrange - Create an agent first
            var agentName = $"ChatTestAgent_{DateTime.Now:yyyyMMdd_HHmmss}";
            var createdAgent = await _apiService.CreateAgentAsync(agentName);
            _testAgentId = createdAgent!.Id;

            var testMessage = "Hello! Please respond with a simple greeting.";

            // Act
            var response = await _apiService.SendMessageAsync(createdAgent.Id, testMessage);

            // Assert
            Assert.That(response, Is.Not.Null, "Message response should not be null");
            Assert.That(response.AgentId, Is.EqualTo(createdAgent.Id), "Response agent ID should match");
            Assert.That(response.ThreadId, Is.Not.Null.And.Not.Empty, "Thread ID should be provided");
            Assert.That(response.Text, Is.Not.Null.And.Not.Empty, "Response text should not be empty");

            Console.WriteLine($"💬 Sent: {testMessage}");
            Console.WriteLine($"🤖 Received: {response.Text}");
            Console.WriteLine($"🧵 Thread ID: {response.ThreadId}");
        }

        [Test]
        [Category(TestCategories.Api)]
        [SlowTest("This test involves actual AI agent processing which can take time")]
        public async Task LiveServer_ContinueConversation_MaintainsContext()
        {
            // Arrange - Create an agent first
            var agentName = $"ConversationTestAgent_{DateTime.Now:yyyyMMdd_HHmmss}";
            var createdAgent = await _apiService.CreateAgentAsync(agentName);
            _testAgentId = createdAgent!.Id;

            // First message
            var firstMessage = "My name is TestUser. Please remember this.";
            var firstResponse = await _apiService.SendMessageAsync(createdAgent.Id, firstMessage);

            // Act - Second message using the same thread
            var secondMessage = "What is my name?";
            var secondResponse = await _apiService.SendMessageAsync(
                createdAgent.Id, 
                secondMessage, 
                firstResponse!.ThreadId);

            // Assert
            Assert.That(secondResponse, Is.Not.Null, "Second response should not be null");
            Assert.That(secondResponse.ThreadId, Is.EqualTo(firstResponse.ThreadId), "Thread ID should be maintained");
            Assert.That(secondResponse.Text.ToLower(), Does.Contain("testuser"), "Agent should remember the name");

            Console.WriteLine($"💬 First: {firstMessage}");
            Console.WriteLine($"🤖 Response: {firstResponse.Text}");
            Console.WriteLine($"💬 Second: {secondMessage}");
            Console.WriteLine($"🤖 Response: {secondResponse.Text}");
            Console.WriteLine($"🧵 Thread maintained: {firstResponse.ThreadId}");
        }

        [Test]
        [Category(TestCategories.Api)]
        [SlowTest("This test involves streaming which can take time")]
        public async Task LiveServer_StreamingMessage_ReceivesIncrementalResponses()
        {
            // Arrange - Create an agent first
            var agentName = $"StreamTestAgent_{DateTime.Now:yyyyMMdd_HHmmss}";
            var createdAgent = await _apiService.CreateAgentAsync(agentName);
            _testAgentId = createdAgent!.Id;

            var testMessage = "Please count from 1 to 5, saying each number on a new line.";
            var receivedChunks = new List<string>();
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            // Act
            var threadId = await _apiService.SendMessageStreamWithThreadAsync(
                createdAgent.Id,
                testMessage,
                null,
                chunk =>
                {
                    receivedChunks.Add(chunk);
                    Console.Write(chunk); // Show streaming in real-time
                },
                cancellationTokenSource.Token);

            // Assert
            Assert.That(receivedChunks.Count, Is.GreaterThan(0), "Should receive at least one chunk");
            Assert.That(threadId, Is.Not.Null.And.Not.Empty, "Should receive a thread ID");
            
            var fullResponse = string.Join("", receivedChunks);
            Assert.That(fullResponse, Is.Not.Empty, "Combined response should not be empty");

            Console.WriteLine($"\n💬 Sent: {testMessage}");
            Console.WriteLine($"🔄 Received {receivedChunks.Count} streaming chunks");
            Console.WriteLine($"📝 Full response: {fullResponse}");
            Console.WriteLine($"🧵 Thread ID: {threadId}");
            
            // Log each chunk for debugging
            for (int i = 0; i < receivedChunks.Count; i++)
            {
                Console.WriteLine($"📦 Chunk {i + 1}: '{receivedChunks[i]}'");
            }
        }

        [Test]
        [Category(TestCategories.Integration)]
        public async Task LiveServer_CompleteWorkflow_CreateAgentAndChat()
        {
            // Arrange
            var agentName = $"WorkflowTestAgent_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Act 1: Create agent
            var createdAgent = await _apiService.CreateAgentAsync(agentName);
            _testAgentId = createdAgent!.Id;

            // Act 2: Verify agent exists
            var retrievedAgent = await _apiService.GetAgentAsync(createdAgent.Id);

            // Act 3: Send a message
            var message = "Hello! Can you tell me what you are?";
            var response = await _apiService.SendMessageAsync(createdAgent.Id, message);

            // Act 4: Verify agent appears in agent list
            var allAgents = await _apiService.GetAgentsAsync();

            // Assert
            Assert.That(createdAgent.Name, Is.EqualTo(agentName));
            Assert.That(retrievedAgent, Is.Not.Null);
            Assert.That(retrievedAgent.Id, Is.EqualTo(createdAgent.Id));
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Text, Is.Not.Empty);
            Assert.That(allAgents, Contains.Item(createdAgent.Id));

            Console.WriteLine($"🔄 Complete workflow test successful:");
            Console.WriteLine($"   ✅ Created agent: {createdAgent.Name}");
            Console.WriteLine($"   ✅ Retrieved agent: {retrievedAgent.Name}");
            Console.WriteLine($"   ✅ Sent message: {message}");
            Console.WriteLine($"   ✅ Received response: {response.Text.Substring(0, Math.Min(response.Text.Length, 50))}...");
            Console.WriteLine($"   ✅ Agent found in list ({allAgents.Count} total agents)");
        }

        [Test]
        [Category(TestCategories.Network)]
        public async Task LiveServer_NetworkErrorHandling_HandlesTimeouts()
        {
            // Arrange - Create a service with very short timeout
            var services = new ServiceCollection();
            services.AddSingleton<ISecureStorageService>(provider => new TestSecureStorageService());
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddHttpClient<IAgentApiService, AgentApiService>(client =>
            {
                client.BaseAddress = new Uri(_configService.ApiBaseUrl);
                client.Timeout = TimeSpan.FromMilliseconds(1); // Extremely short timeout
            });

            using var shortTimeoutProvider = services.BuildServiceProvider();
            var shortTimeoutApiService = shortTimeoutProvider.GetRequiredService<IAgentApiService>();

            // Act & Assert
            var agents = await shortTimeoutApiService.GetAgentsAsync();
            
            // The service should handle the timeout gracefully and return an empty list
            Assert.That(agents, Is.Not.Null, "Should return empty list, not null");
            Assert.That(agents, Is.Empty, "Should return empty list on timeout");

            Console.WriteLine("✅ Network timeout handled gracefully - returned empty list");
        }

        private async Task CleanupCreatedAgents()
        {
            Console.WriteLine($"🧹 Cleaning up {_createdAgentIds.Count} test agents...");
            
            // Note: The current API doesn't have a delete endpoint
            // This is a placeholder for when that functionality is added
            foreach (var agentId in _createdAgentIds)
            {
                Console.WriteLine($"   📝 Test agent to cleanup: {agentId}");
                // TODO: Delete agent when API supports it
                // await _apiService.DeleteAgentAsync(agentId);
            }
            
            Console.WriteLine("ℹ️ Note: Agent cleanup requires manual deletion as API doesn't support delete yet");
        }

        /// <summary>
        /// Simple in-memory storage for testing that doesn't persist data
        /// </summary>
        private class TestSecureStorageService : ISecureStorageService
        {
            private readonly Dictionary<string, string> _storage = new();

            public Task<string?> GetAsync(string key)
            {
                _storage.TryGetValue(key, out var value);
                return Task.FromResult(value);
            }

            public Task SetAsync(string key, string value)
            {
                _storage[key] = value;
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                _storage.Remove(key);
            }
        }
    }
}