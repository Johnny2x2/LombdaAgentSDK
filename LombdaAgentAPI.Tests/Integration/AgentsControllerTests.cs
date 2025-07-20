using LombdaAgentAPI.Models;
using System.Net;
using System.Text.Json;

namespace LombdaAgentAPI.Tests.Integration
{
    [TestFixture]
    public class AgentsControllerTests : TestBase
    {
        [Test]
        public async Task GetAgents_ReturnsEmptyList_WhenNoAgentsExist()
        {
            // Act
            var response = await _client.GetAsync("/v1/agents");
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            var content = await response.Content.ReadAsStringAsync();
            var agents = JsonSerializer.Deserialize<List<string>>(content);
            
            Assert.That(agents, Is.Not.Null);
            Assert.That(agents, Is.Empty);
        }

        [Test]
        public async Task CreateAgent_ReturnsNewAgent_WithValidRequest()
        {
            // Arrange
            var request = new AgentCreationRequest { Name = "TestAgent" };
            
            // Act
            var response = await _client.PostAsync("/v1/agents", SerializeToJson(request));
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            var agent = await DeserializeResponse<AgentResponse>(response);
            Assert.That(agent, Is.Not.Null);
            Assert.That(agent.Name, Is.EqualTo("TestAgent"));
            Assert.That(agent.Id, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task GetAgent_ReturnsAgent_WhenAgentExists()
        {
            // Arrange
            var createRequest = new AgentCreationRequest { Name = "TestAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Act
            var response = await _client.GetAsync($"/v1/agents/{createdAgent.Id}");
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            var agent = await DeserializeResponse<AgentResponse>(response);
            Assert.That(agent, Is.Not.Null);
            Assert.That(agent.Id, Is.EqualTo(createdAgent.Id));
            Assert.That(agent.Name, Is.EqualTo("TestAgent"));
        }

        [Test]
        public async Task GetAgent_Returns404_WhenAgentDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync($"/v1/agents/nonexistent-id");
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task SendMessage_ReturnsResponse_WhenAgentExists()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "MessageAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Arrange - Create message request
            var messageRequest = new MessageRequest { Text = "Hello, how are you?" };
            
            // Act
            var response = await _client.PostAsync($"/v1/agents/{createdAgent.Id}/messages", SerializeToJson(messageRequest));
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            var messageResponse = await DeserializeResponse<MessageResponse>(response);
            Assert.That(messageResponse, Is.Not.Null);
            Assert.That(messageResponse.AgentId, Is.EqualTo(createdAgent.Id));
            Assert.That(messageResponse.ThreadId, Is.Not.Null.And.Not.Empty);
            Assert.That(messageResponse.Text, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task SendMessage_Returns404_WhenAgentDoesNotExist()
        {
            // Arrange
            var messageRequest = new MessageRequest { Text = "Hello" };
            
            // Act
            var response = await _client.PostAsync($"/v1/agents/nonexistent-id/messages", SerializeToJson(messageRequest));
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task SendMessageWithThread_UsesExistingThread_WhenThreadIdProvided()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "ThreadAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Arrange - Send first message to get a thread ID
            var firstMessageRequest = new MessageRequest { Text = "What is 2+2?" };
            var firstResponse = await _client.PostAsync($"/v1/agents/{createdAgent.Id}/messages", SerializeToJson(firstMessageRequest));
            var firstMessageResponse = await DeserializeResponse<MessageResponse>(firstResponse);
            
            // Arrange - Create second message request with thread ID
            var secondMessageRequest = new MessageRequest 
            { 
                Text = "And what is 3+3?",
                ThreadId = firstMessageResponse.ThreadId
            };
            
            // Act
            var response = await _client.PostAsync($"/v1/agents/{createdAgent.Id}/messages", SerializeToJson(secondMessageRequest));
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            var messageResponse = await DeserializeResponse<MessageResponse>(response);
            Assert.That(messageResponse, Is.Not.Null);
            Assert.That(messageResponse.ThreadId, Is.EqualTo(firstMessageResponse.ThreadId));
            Assert.That(messageResponse.Text, Is.Not.Null.And.Not.Empty);
        }
    }
}