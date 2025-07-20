using LombdaAgentAPI.Models;
using System.Net;
using System.Text;

namespace LombdaAgentAPI.Tests.Integration
{
    [TestFixture]
    public class StreamControllerTests : TestBase
    {
        [Test]
        public async Task StreamAgentEvents_Returns404_WhenAgentDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync("/v1/stream/agents/nonexistent-id");
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task StreamAgentEvents_ReturnsEventStream_WhenAgentExists()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "StreamingAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);

            // Act - Start the event stream connection
            // For testing SSE, we'll use a special request setup
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/stream/agents/{createdAgent.Id}");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
        }

        [Test]
        public async Task MessageStream_Returns404_WhenAgentDoesNotExist()
        {
            // Arrange
            var messageRequest = new MessageRequest { Text = "Hello" };
            
            // Act
            var response = await _client.PostAsync("/v1/agents/nonexistent-id/messages/stream", SerializeToJson(messageRequest));
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MessageStream_ReturnsEventStream_WhenAgentExists()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "MessageStreamingAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Arrange - Create message request
            var messageRequest = new MessageRequest { Text = "Tell me a short joke" };
            
            // Act - Send the streaming message request
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/agents/{createdAgent.Id}/messages/stream")
            {
                Content = SerializeToJson(messageRequest)
            };
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
        }
    }
}