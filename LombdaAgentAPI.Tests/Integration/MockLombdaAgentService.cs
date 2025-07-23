using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Hubs;
using LombdaAgentAPI.Tests.Mocks;
using LombdaAgentSDK.AgentStateSystem;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LombdaAgentAPI.Tests.Integration
{
    /// <summary>
    /// A mock implementation of ILombdaAgentService that uses MockLombdaAgent
    /// </summary>
    public class MockLombdaAgentService : ILombdaAgentService
    {
        private readonly ConcurrentDictionary<string, Type> _agentTypes = new();
        private readonly ConcurrentDictionary<string, LombdaAgent> _agents = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _agentConnections = new();
        private readonly ConcurrentDictionary<string, string> _connectionToAgentMap = new();
        private readonly IHubContext<AgentHub> _hubContext;

        public MockLombdaAgentService(IHubContext<AgentHub> hubContext)
        {
            _hubContext = hubContext;
            
            // Register the mock agent type
            RegisterAgentType("Mock", typeof(MockLombdaAgent));
            RegisterAgentType("Default", typeof(MockLombdaAgent));
        }

        public List<string> GetAgentIds()
        {
            return _agents.Keys.ToList();
        }

        public List<string> GetAgentTypes()
        {
            return _agentTypes.Keys.ToList();
        }

        public bool RegisterAgentType(string typeName, Type agentType)
        {
            // Validate that the type inherits from LombdaAgent
            if (!typeof(LombdaAgent).IsAssignableFrom(agentType))
            {
                return false;
            }

            return _agentTypes.TryAdd(typeName, agentType);
        }

        public LombdaAgent? GetAgent(string agentId)
        {
            return _agents.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public string CreateAgent(string agentName, string agentType = "Mock")
        {
            var agentId = Guid.NewGuid().ToString();
            var agent = new MockLombdaAgent(agentId, agentName);
            
            // Subscribe to agent events
            agent.RootStreamingEvent += async (message) => 
            {
                if (_agentConnections.TryGetValue(agentId, out var connections))
                {
                    foreach (var connectionId in connections)
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveAgentStream", agentId, message);
                    }
                }
            };

            _agents.TryAdd(agentId, agent);
            _agentConnections.TryAdd(agentId, new ConcurrentBag<string>());
            
            return agentId;
        }

        public bool AddStreamingSubscriber(string agentId, string connectionId)
        {
            if (!_agents.ContainsKey(agentId))
                return false;

            if (!_agentConnections.TryGetValue(agentId, out var connections))
            {
                connections = new ConcurrentBag<string>();
                _agentConnections.TryAdd(agentId, connections);
            }

            connections.Add(connectionId);
            _connectionToAgentMap.TryAdd(connectionId, agentId);
            
            return true;
        }

        public void RemoveStreamingSubscriber(string connectionId)
        {
            if (_connectionToAgentMap.TryRemove(connectionId, out var agentId))
            {
                if (_agentConnections.TryGetValue(agentId, out var connections))
                {
                    // Create a new bag without the removed connection
                    var newConnections = new ConcurrentBag<string>(connections.Where(c => c != connectionId));
                    _agentConnections.TryUpdate(agentId, newConnections, connections);
                }
            }
        }
    }
}