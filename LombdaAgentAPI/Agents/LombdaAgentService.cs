using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentAPI.Hubs;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LombdaAgentAPI.Agents
{
    /// <summary>
    /// Provides services for managing and interacting with Lombda agents, including creating agents, retrieving agent
    /// information, and managing streaming subscribers.
    /// </summary>
    /// <remarks>This service allows clients to create agents, retrieve a list of agent IDs, and manage
    /// streaming subscribers for real-time communication. It uses a hub context to facilitate communication with
    /// connected clients.</remarks>
    public class LombdaAgentService : ILombdaAgentService
    {
        private readonly ConcurrentDictionary<string, LombdaAgent> _agents = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _agentConnections = new();
        private readonly ConcurrentDictionary<string, string> _connectionToAgentMap = new();
        private readonly IHubContext<AgentHub> _hubContext;

        public LombdaAgentService(IHubContext<AgentHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public List<string> GetAgentIds()
        {
            return _agents.Keys.ToList();
        }

        public LombdaAgent? GetAgent(string agentId)
        {
            return _agents.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public string CreateAgent(string agentName)
        {
            var agentId = Guid.NewGuid().ToString();
            var agent = new APILombdaAgent(agentId, agentName);
            
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

    /// <summary>
    /// Implementation of LombdaAgent for API use
    /// </summary>
    public class APILombdaAgent : LombdaAgent
    {
        private readonly string _agentId;
        private readonly string _agentName;

        public string AgentId => _agentId;
        public string AgentName => _agentName;

        public APILombdaAgent(string agentId, string agentName) : base()
        {
            _agentId = agentId;
            _agentName = agentName;
        }

        public override void InitializeAgent()
        {
            // Initialize with OpenAI client
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            }

            LLMTornadoModelProvider client = new(
                ChatModel.OpenAi.Gpt41.V41, 
                [new ProviderAuthentication(LLmProviders.OpenAi, apiKey),], 
                useResponseAPI: true
            );
            
            string instructions = $"You are an assistant named {_agentName}. Be helpful, concise, and clear in your responses.";
            ControlAgent = new Agent(client, _agentName, instructions);
            
            Console.WriteLine($"[AGENT DEBUG] APILombdaAgent {_agentId} initialized with proper streaming integration");
        }
    }
}