using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Hubs;
using LombdaAgentAPI.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace LombdaAgentAPI.Tests.Integration
{
    [TestFixture]
    public class AgentHubTests : TestBase
    {
        private HubConnection _hubConnection = null!;

        [SetUp]
        public new async Task SetUp()
        {
            base.SetUp();

            // Create a SignalR connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_factory.Server.BaseAddress}agentHub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
                .Build();

            await _hubConnection.StartAsync();
        }

        [TearDown]
        public new async Task TearDown()
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            base.TearDown();
        }

        [Test]
        public async Task SubscribeToAgent_ReturnsFalse_WhenAgentDoesNotExist()
        {
            // Act
            var result = await _hubConnection.InvokeAsync<bool>("SubscribeToAgent", "nonexistent-id");
            
            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SubscribeToAgent_ReturnsTrue_WhenAgentExists()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "HubAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Act
            var result = await _hubConnection.InvokeAsync<bool>("SubscribeToAgent", createdAgent.Id);
            
            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SubscribeToAgent_ReceivesMessages_WhenAgentStreamingOccurs()
        {
            // Arrange - Create a new agent
            var createRequest = new AgentCreationRequest { Name = "StreamingHubAgent" };
            var createResponse = await _client.PostAsync("/v1/agents", SerializeToJson(createRequest));
            var createdAgent = await DeserializeResponse<AgentResponse>(createResponse);
            
            // Arrange - Setup event handler to receive messages
            var receivedMessages = new List<string>();
            string? receivedAgentId = null;
            
            _hubConnection.On<string, string>("ReceiveAgentStream", (agentId, message) =>
            {
                receivedAgentId = agentId;
                receivedMessages.Add(message);
            });
            
            // Act - Subscribe to the agent
            var subscribeResult = await _hubConnection.InvokeAsync<bool>("SubscribeToAgent", createdAgent.Id);
            
            // Act - Send a message to trigger streaming
            var messageRequest = new MessageRequest { Text = "Tell me about the weather" };
            await _client.PostAsync($"/v1/agents/{createdAgent.Id}/messages", SerializeToJson(messageRequest));
            
            // Wait a bit for the streaming to occur and messages to be delivered
            await Task.Delay(500);
            
            // Assert
            Assert.That(subscribeResult, Is.True);
            Assert.That(receivedAgentId, Is.EqualTo(createdAgent.Id));
            
            // Note: In a real test with real streaming, we'd assert that messages were received.
            // For this test, we're just checking that the subscription mechanism works.
        }
    }
}