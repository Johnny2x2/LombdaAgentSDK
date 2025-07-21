using System.Net;
using System.Text;
using System.Text.Json;
using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace LombdaAgentMAUI.Tests.Integration
{
    [TestFixture]
    public class ServiceIntegrationTests
    {
        private ServiceProvider _serviceProvider;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private JsonSerializerOptions _jsonOptions;

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var services = new ServiceCollection();
            
            // Register mock secure storage
            services.AddSingleton<ISecureStorageService, MockSecureStorageService>();
            
            // Register configuration service
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Register HTTP client with mock handler
            services.AddTransient<HttpClient>(provider =>
            {
                var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
                {
                    BaseAddress = new Uri("https://test.example.com/")
                };
                return httpClient;
            });
            
            // Register API service
            services.AddTransient<IAgentApiService, AgentApiService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        [Test]
        public async Task ConfigurationAndApiService_WorkTogether()
        {
            // Arrange
            var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
            var apiService = _serviceProvider.GetRequiredService<IAgentApiService>();

            configService.ApiBaseUrl = "https://test-api.example.com/";
            await configService.SaveSettingsAsync();

            var expectedAgents = new List<string> { "agent1", "agent2" };
            var jsonResponse = JsonSerializer.Serialize(expectedAgents, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var agents = await apiService.GetAgentsAsync();

            // Assert
            Assert.That(agents, Is.Not.Null);
            Assert.That(agents.Count, Is.EqualTo(2));
            Assert.That(configService.ApiBaseUrl, Is.EqualTo("https://test-api.example.com/"));
        }

        [Test]
        public async Task CompleteAgentWorkflow_CreateGetSendMessage()
        {
            // Arrange
            var apiService = _serviceProvider.GetRequiredService<IAgentApiService>();

            // Setup responses for the workflow
            var createResponse = new AgentResponse { Id = "new-agent-id", Name = "Test Agent" };
            var getResponse = new AgentResponse { Id = "new-agent-id", Name = "Test Agent" };
            var messageResponse = new MessageResponse 
            { 
                AgentId = "new-agent-id", 
                ThreadId = "thread-123", 
                Text = "Hello! How can I help you?" 
            };

            // Setup sequential responses
            _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(createResponse, _jsonOptions), Encoding.UTF8, "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(getResponse, _jsonOptions), Encoding.UTF8, "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(messageResponse, _jsonOptions), Encoding.UTF8, "application/json")
                });

            // Act
            // 1. Create agent
            var createdAgent = await apiService.CreateAgentAsync("Test Agent");
            
            // 2. Get agent
            var retrievedAgent = await apiService.GetAgentAsync(createdAgent!.Id);
            
            // 3. Send message
            var response = await apiService.SendMessageAsync(createdAgent.Id, "Hello");

            // Assert
            Assert.That(createdAgent, Is.Not.Null);
            Assert.That(createdAgent.Name, Is.EqualTo("Test Agent"));
            
            Assert.That(retrievedAgent, Is.Not.Null);
            Assert.That(retrievedAgent.Id, Is.EqualTo(createdAgent.Id));
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.AgentId, Is.EqualTo(createdAgent.Id));
            Assert.That(response.ThreadId, Is.Not.Null.And.Not.Empty);
            Assert.That(response.Text, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task ConfigurationPersistence_AcrossServiceInstances()
        {
            // Arrange
            var originalConfigService = _serviceProvider.GetRequiredService<IConfigurationService>();
            var testUrl = "https://persistent-test.example.com/";

            // Act
            originalConfigService.ApiBaseUrl = testUrl;
            await originalConfigService.SaveSettingsAsync();

            // Create a new service instance with the same storage (simulates app restart)
            var mockStorage = _serviceProvider.GetRequiredService<ISecureStorageService>();
            var newConfigService = new ConfigurationService(mockStorage);
            await newConfigService.LoadSettingsAsync();

            // Assert
            Assert.That(newConfigService.ApiBaseUrl, Is.EqualTo(testUrl));
        }

        [Test]
        public async Task ApiService_HandlesMultipleSimultaneousRequests()
        {
            // Arrange
            var apiService = _serviceProvider.GetRequiredService<IAgentApiService>();
            var agentResponse = new AgentResponse { Id = "test-agent", Name = "Test Agent" };
            var jsonResponse = JsonSerializer.Serialize(agentResponse, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var tasks = new List<Task<AgentResponse?>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(apiService.CreateAgentAsync($"Agent {i}"));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.That(results.Length, Is.EqualTo(5));
            Assert.That(results.All(r => r != null), Is.True);
            Assert.That(results.All(r => r!.Id == "test-agent"), Is.True);
        }

        [Test]
        public async Task ErrorHandling_ServicesContinueToWorkAfterErrors()
        {
            // Arrange
            var apiService = _serviceProvider.GetRequiredService<IAgentApiService>();

            // Setup error response first, then success
            _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Server Error")
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new List<string> { "agent1" }, _jsonOptions))
                });

            // Act
            var firstCall = await apiService.GetAgentsAsync(); // Should fail gracefully
            var secondCall = await apiService.GetAgentsAsync(); // Should succeed

            // Assert
            Assert.That(firstCall, Is.Empty); // Error returns empty list
            Assert.That(secondCall, Has.Count.EqualTo(1)); // Success returns data
        }

        [Test]
        public void ServiceRegistration_AllServicesCanBeResolved()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
                var apiService = _serviceProvider.GetRequiredService<IAgentApiService>();
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
                var secureStorage = _serviceProvider.GetRequiredService<ISecureStorageService>();

                Assert.That(configService, Is.Not.Null);
                Assert.That(apiService, Is.Not.Null);
                Assert.That(httpClient, Is.Not.Null);
                Assert.That(secureStorage, Is.Not.Null);
            });
        }

        [Test]
        public async Task ChatMessageFlow_EndToEnd()
        {
            // Arrange
            var apiService = _serviceProvider.GetRequiredService<IAgentApiService>();
            var configService = _serviceProvider.GetRequiredService<IConfigurationService>();

            // Setup configuration
            configService.ApiBaseUrl = "https://chat-test.example.com/";

            // Setup API responses
            var agentListResponse = new List<string> { "chat-agent-1" };
            var messageResponse = new MessageResponse
            {
                AgentId = "chat-agent-1",
                ThreadId = "chat-thread-1",
                Text = "Hi! I'm ready to chat."
            };

            _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(agentListResponse, _jsonOptions))
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(messageResponse, _jsonOptions))
                });

            // Act - Simulate chat flow
            var availableAgents = await apiService.GetAgentsAsync();
            var chatResponse = await apiService.SendMessageAsync(availableAgents.First(), "Hello!");

            // Create chat message models
            var userMessage = new ChatMessage
            {
                Text = "Hello!",
                IsUser = true,
                Timestamp = DateTime.Now
            };

            var agentMessage = new ChatMessage
            {
                Text = chatResponse!.Text,
                IsUser = false,
                Timestamp = DateTime.Now
            };

            // Assert
            Assert.That(availableAgents, Has.Count.EqualTo(1));
            Assert.That(chatResponse, Is.Not.Null);
            Assert.That(chatResponse.Text, Is.EqualTo("Hi! I'm ready to chat."));
            
            // Verify chat message models work correctly
            Assert.That(userMessage.IsUser, Is.True);
            Assert.That(agentMessage.IsUser, Is.False);
            Assert.That(userMessage.DisplayTime, Is.Not.Null.And.Not.Empty);
            Assert.That(agentMessage.DisplayTime, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task SecureStorage_PersistsDataCorrectly()
        {
            // Arrange
            var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
            var secureStorage = _serviceProvider.GetRequiredService<ISecureStorageService>();
            var testUrl = "https://storage-test.example.com/";

            // Act
            configService.ApiBaseUrl = testUrl;
            await configService.SaveSettingsAsync();

            // Verify storage directly
            var storedValue = await secureStorage.GetAsync("api_base_url");

            // Assert
            Assert.That(storedValue, Is.EqualTo(testUrl));
        }
    }
}