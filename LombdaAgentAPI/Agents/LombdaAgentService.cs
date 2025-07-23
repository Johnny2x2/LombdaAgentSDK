using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentAPI.Hubs;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
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
        private readonly ConcurrentDictionary<string, Type> _agentTypes = new();
        private readonly ConcurrentDictionary<string, LombdaAgent> _agents = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _agentConnections = new();
        private readonly ConcurrentDictionary<string, string> _connectionToAgentMap = new();
        private readonly IHubContext<AgentHub> _hubContext;

        public LombdaAgentService(IHubContext<AgentHub> hubContext)
        {
            _hubContext = hubContext;
            
            // Register available agent types - you can register your custom agent types here
            RegisterAgentType("Default", typeof(APILombdaAgent));
            RegisterAgentType("CodeAssistant", typeof(CodeAssistantLombdaAgent));
            RegisterAgentType("Creative", typeof(CreativeLombdaAgent));
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

            // Validate that the type has a constructor that accepts a string parameter (agentName)
            var constructors = agentType.GetConstructors();
            var hasValidConstructor = constructors.Any(c => 
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
            });

            if (!hasValidConstructor)
            {
                return false;
            }

            return _agentTypes.TryAdd(typeName, agentType);
        }

        public LombdaAgent? GetAgent(string agentId)
        {
            return _agents.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public string CreateAgent(string agentName, string agentType)
        {
            if(_agentTypes.TryGetValue(agentType, out var type) == false)
            {
                return "0";
            }
            else
            {
                var agent = Activator.CreateInstance(type, [agentName]) as LombdaAgent;

                if(agent == null)
                {
                    return "0";
                }

                // Subscribe to agent events
                agent.RootStreamingEvent += async (message) =>
                {
                    if (_agentConnections.TryGetValue(agent.AgentId, out var connections))
                    {
                        foreach (var connectionId in connections)
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveAgentStream", agent.AgentId, message);
                        }
                    }
                };

                _agents.TryAdd(agent.AgentId, agent);
                _agentConnections.TryAdd(agent.AgentId, new ConcurrentBag<string>());

                return agent.AgentId;
            }
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
        public APILombdaAgent(string agentName) : base(agentName)
        {
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
            
            string instructions = $"You are an assistant named {AgentName}. Be helpful, concise, and clear in your responses.";
            ControlAgent = new Agent(client, AgentName, instructions);
            
            Console.WriteLine($"[AGENT DEBUG] APILombdaAgent {AgentId} initialized with proper streaming integration");
        }
    }

    /// <summary>
    /// Specialized agent for code-related tasks
    /// </summary>
    public class CodeAssistantLombdaAgent : LombdaAgent
    {
        public CodeAssistantLombdaAgent(string agentName) : base(agentName)
        {
        }

        public override void InitializeAgent()
        {
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
            
            string instructions = $"You are {AgentName}, a specialized coding assistant. You excel at helping with programming tasks, code review, debugging, and software architecture. Provide clear, well-commented code examples and explain complex concepts in an understandable way.";
            ControlAgent = new Agent(client, AgentName, instructions);
            
            Console.WriteLine($"[AGENT DEBUG] CodeAssistantLombdaAgent {AgentId} initialized");
        }
    }

    /// <summary>
    /// Specialized agent for creative tasks
    /// </summary>
    public class CreativeLombdaAgent : LombdaAgent
    {
        public CreativeLombdaAgent(string agentName) : base(agentName)
        {
        }

        public override void InitializeAgent()
        {
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
            
            string instructions = $"You are {AgentName}, a creative assistant specializing in writing, storytelling, brainstorming, and artistic endeavors. You have a vibrant imagination and help users with creative projects, content creation, and innovative thinking. Be expressive and engaging in your responses.";
            ControlAgent = new Agent(client, AgentName, instructions);
            
            Console.WriteLine($"[AGENT DEBUG] CreativeLombdaAgent {AgentId} initialized");
        }
    }
}