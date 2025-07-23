using LombdaAgentAPI.Models;
using System.Net;
using System.Text;
using System.Text.Json;

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

        [Test]
        public async Task MessageStream_ReturnsModelStreamingEvents_WhenSendingMessage()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "EventTestAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Arrange - Create message request
            var messageRequest = new MessageRequest { Text = "Say hello" };
            
            // Act - Send the streaming message request and read events
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/agents/{createdAgent.Id}/messages/stream")
            {
                Content = SerializeToJson(messageRequest)
            };
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var events = new List<(string eventType, string data)>();
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
            
            string? currentEvent = null;
            var hasConnectedEvent = false;
            var hasDeltaEvent = false;
            var hasCompleteEvent = false;

            // Read the stream until we get a complete event or timeout
            while (!reader.EndOfStream && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("event: "))
                {
                    currentEvent = line.Substring(7).Trim();
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (currentEvent != null)
                    {
                        events.Add((currentEvent, data));
                        
                        // Track specific events we expect
                        switch (currentEvent)
                        {
                            case "connected":
                                hasConnectedEvent = true;
                                break;
                            case "delta":
                                hasDeltaEvent = true;
                                break;
                            case "complete":
                                hasCompleteEvent = true;
                                break;
                        }
                        
                        // Exit early if we've seen a complete event
                        if (currentEvent == "complete")
                        {
                            break;
                        }
                    }
                }
                else if (string.IsNullOrEmpty(line))
                {
                    currentEvent = null;
                }
            }

            // Assert - Verify we received the expected events
            Assert.That(events.Count, Is.GreaterThan(0), "Should receive at least one event");
            Assert.That(hasConnectedEvent, Is.True, "Should receive a 'connected' event");
            
            // Note: We might not always get delta events for very short responses, so this is optional
            // Assert.That(hasDeltaEvent, Is.True, "Should receive at least one 'delta' event");
            
            Assert.That(hasCompleteEvent, Is.True, "Should receive a 'complete' event");

            // Verify the complete event contains expected data structure
            var completeEvent = events.Where(e => e.eventType == "complete").FirstOrDefault();
            Assert.That(completeEvent.eventType, Is.Not.Null, "Should have a complete event");
            
            try
            {
                // Try to parse the complete event data as JSON
                var completeData = JsonSerializer.Deserialize<MessageResponse>(completeEvent.data, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true 
                });
                
                Assert.That(completeData, Is.Not.Null, "Complete event data should be parseable as MessageResponse");
                Assert.That(completeData.ThreadId, Is.Not.Null.And.Not.Empty, "Complete event should contain a ThreadId");
                Assert.That(completeData.Text, Is.Not.Null, "Complete event should contain response text");
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Complete event data should be valid JSON: {ex.Message}. Data was: {completeEvent.data}");
            }
        }

        [Test]
        public async Task TestStreamingEvents_ReturnsTestEvents()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "TestEventAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Act - Call the test streaming endpoint
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/agents/{createdAgent.Id}/test-stream");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var events = new List<(string eventType, string data)>();
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10 second timeout
            
            string? currentEvent = null;
            var messageCount = 0;

            // Read the stream until we get all test events or timeout
            while (!reader.EndOfStream && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("event: "))
                {
                    currentEvent = line.Substring(7).Trim();
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (currentEvent != null)
                    {
                        events.Add((currentEvent, data));
                        
                        if (currentEvent == "message")
                        {
                            messageCount++;
                        }
                        
                        // Exit after complete event
                        if (currentEvent == "complete")
                        {
                            break;
                        }
                    }
                }
                else if (string.IsNullOrEmpty(line))
                {
                    currentEvent = null;
                }
            }

            // Assert - Verify we received the expected test events
            Assert.That(events.Count, Is.GreaterThan(0), "Should receive test events");
            Assert.That(messageCount, Is.EqualTo(5), "Should receive exactly 5 test message events");
            
            var completeEvents = events.Where(e => e.eventType == "complete").ToList();
            Assert.That(completeEvents.Count, Is.EqualTo(1), "Should receive exactly one complete event");
        }
    }
}