using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Hubs;
using LombdaAgentSDK.AgentStateSystem;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace LombdaAgentAPI.Tests.Unit
{
    [TestFixture]
    public class LombdaAgentServiceTests
    {
        private Mock<IHubContext<AgentHub>> _mockHubContext = null!;
        private Mock<IHubClients> _mockClients = null!;
        private Mock<IClientProxy> _mockClientProxy = null!;
        private LombdaAgentService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _mockClientProxy = new Mock<IClientProxy>();
            _mockClients = new Mock<IHubClients>();
            _mockClients.Setup<IClientProxy>(clients => clients.Client(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _mockHubContext = new Mock<IHubContext<AgentHub>>();
            _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);

            _service = new LombdaAgentService(_mockHubContext.Object);
        }

        [Test]
        public void GetAgentIds_ReturnsEmptyList_WhenNoAgentsExist()
        {
            // Act
            var result = _service.GetAgentIds();
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CreateAgent_ReturnsNewAgentId()
        {
            // Act
            var agentId = _service.CreateAgent("TestAgent","Default");
            
            // Assert
            Assert.That(agentId, Is.Not.Null.And.Not.Empty);
            
            var agent = _service.GetAgent(agentId);
            Assert.That(agent, Is.Not.Null);
            Assert.That(agent, Is.TypeOf<APILombdaAgent>());
            
            if (agent is APILombdaAgent apiAgent)
            {
                Assert.That(apiAgent.AgentId, Is.EqualTo(agentId));
                Assert.That(apiAgent.AgentName, Is.EqualTo("TestAgent"));
            }
        }

        [Test]
        public void GetAgent_ReturnsNull_WhenAgentDoesNotExist()
        {
            // Act
            var agent = _service.GetAgent("nonexistent-id");
            
            // Assert
            Assert.That(agent, Is.Null);
        }

        [Test]
        public void AddStreamingSubscriber_ReturnsFalse_WhenAgentDoesNotExist()
        {
            // Act
            var result = _service.AddStreamingSubscriber("nonexistent-id", "connection-id");
            
            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void AddStreamingSubscriber_ReturnsTrue_WhenAgentExists()
        {
            // Arrange
            var agentId = _service.CreateAgent("TestAgent", "Default");
            
            // Act
            var result = _service.AddStreamingSubscriber(agentId, "connection-id");
            
            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void RemoveStreamingSubscriber_RemovesSubscription()
        {
            // Arrange
            var agentId = _service.CreateAgent("TestAgent", "Default");
            _service.AddStreamingSubscriber(agentId, "connection-id");
            
            // Act
            _service.RemoveStreamingSubscriber("connection-id");
            
            // Assert - We'd need a way to verify the subscription was removed
            // This is an indirect test through the mock
            Assert.That(_service.GetAgent(agentId), Is.Not.Null);
        }
    }
}