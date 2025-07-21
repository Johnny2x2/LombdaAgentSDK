using System.Net;
using System.Text;
using System.Text.Json;
using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;
using Moq;
using Moq.Protected;

namespace LombdaAgentMAUI.Tests.Services
{
    [TestFixture]
    public class AgentApiServiceTests
    {
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private AgentApiService _agentApiService;
        private JsonSerializerOptions _jsonOptions;

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:5000/")
            };
            _agentApiService = new AgentApiService(_httpClient);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        [TearDown]
        public void TearDown()
        {
            _agentApiService?.Dispose();
            _httpClient?.Dispose();
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
        public async Task GetAgentsAsync_SuccessfulResponse_ReturnsAgentList()
        {
            // Arrange
            var expectedAgents = new List<string> { "agent1", "agent2", "agent3" };
            var jsonResponse = JsonSerializer.Serialize(expectedAgents, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.GetAgentsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Is.EqualTo(expectedAgents));
        }

        [Test]
        public async Task GetAgentsAsync_HttpError_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

            // Act
            var result = await _agentApiService.GetAgentsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAgentsAsync_InvalidJson_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid json");

            // Act
            var result = await _agentApiService.GetAgentsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task CreateAgentAsync_SuccessfulResponse_ReturnsAgentResponse()
        {
            // Arrange
            var expectedAgent = new AgentResponse { Id = "test-id", Name = "Test Agent" };
            var jsonResponse = JsonSerializer.Serialize(expectedAgent, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.CreateAgentAsync("Test Agent");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo("test-id"));
            Assert.That(result.Name, Is.EqualTo("Test Agent"));
        }

        [Test]
        public async Task CreateAgentAsync_HttpError_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.BadRequest, "Bad Request");

            // Act
            var result = await _agentApiService.CreateAgentAsync("Test Agent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAgentAsync_SuccessfulResponse_ReturnsAgentResponse()
        {
            // Arrange
            var expectedAgent = new AgentResponse { Id = "test-id", Name = "Test Agent" };
            var jsonResponse = JsonSerializer.Serialize(expectedAgent, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.GetAgentAsync("test-id");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo("test-id"));
            Assert.That(result.Name, Is.EqualTo("Test Agent"));
        }

        [Test]
        public async Task GetAgentAsync_NotFound_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _agentApiService.GetAgentAsync("nonexistent-id");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SendMessageAsync_SuccessfulResponse_ReturnsMessageResponse()
        {
            // Arrange
            var expectedResponse = new MessageResponse 
            { 
                AgentId = "agent-id", 
                ThreadId = "thread-id", 
                Text = "Hello, how can I help you?" 
            };
            var jsonResponse = JsonSerializer.Serialize(expectedResponse, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.SendMessageAsync("agent-id", "Hello", "thread-id");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AgentId, Is.EqualTo("agent-id"));
            Assert.That(result.ThreadId, Is.EqualTo("thread-id"));
            Assert.That(result.Text, Is.EqualTo("Hello, how can I help you?"));
        }

        [Test]
        public async Task SendMessageAsync_HttpError_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

            // Act
            var result = await _agentApiService.SendMessageAsync("agent-id", "Hello");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SendMessageAsync_WithoutThreadId_SendsCorrectRequest()
        {
            // Arrange
            var expectedResponse = new MessageResponse 
            { 
                AgentId = "agent-id", 
                ThreadId = "new-thread-id", 
                Text = "Response" 
            };
            var jsonResponse = JsonSerializer.Serialize(expectedResponse, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.SendMessageAsync("agent-id", "Hello");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ThreadId, Is.EqualTo("new-thread-id"));
        }

        [Test]
        public void SendMessageStreamAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel(); // Cancel immediately
            var messagesReceived = new List<string>();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                await _agentApiService.SendMessageStreamAsync(
                    "agent-id",
                    "Hello",
                    null,
                    messagesReceived.Add,
                    cancellationTokenSource.Token);
            });
        }

        [Test]
        [TestCase("")]
        [TestCase("   ")]
        public async Task CreateAgentAsync_WithInvalidName_StillMakesRequest(string agentName)
        {
            // Arrange
            var expectedAgent = new AgentResponse { Id = "test-id", Name = agentName ?? "" };
            var jsonResponse = JsonSerializer.Serialize(expectedAgent, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.CreateAgentAsync(agentName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo("test-id"));
        }

        [Test]
        public async Task CreateAgentAsync_WithNullName_StillMakesRequest()
        {
            // Arrange
            var expectedAgent = new AgentResponse { Id = "test-id", Name = "" };
            var jsonResponse = JsonSerializer.Serialize(expectedAgent, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.CreateAgentAsync(null!);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo("test-id"));
        }

        [Test]
        public async Task GetAgentsAsync_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            var emptyList = new List<string>();
            var jsonResponse = JsonSerializer.Serialize(emptyList, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.GetAgentsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task SendMessageAsync_LongMessage_HandlesCorrectly()
        {
            // Arrange
            var longMessage = new string('a', 10000); // 10KB message
            var expectedResponse = new MessageResponse 
            { 
                AgentId = "agent-id", 
                ThreadId = "thread-id", 
                Text = "Response to long message" 
            };
            var jsonResponse = JsonSerializer.Serialize(expectedResponse, _jsonOptions);
            SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

            // Act
            var result = await _agentApiService.SendMessageAsync("agent-id", longMessage);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Text, Is.EqualTo("Response to long message"));
        }
    }
}