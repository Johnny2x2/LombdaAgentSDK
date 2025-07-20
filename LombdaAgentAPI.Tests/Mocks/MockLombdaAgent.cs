using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK;

namespace LombdaAgentAPI.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of LombdaAgent for testing purposes
    /// </summary>
    public class MockLombdaAgent : LombdaAgent
    {
        private readonly string _agentId;
        private readonly string _agentName;

        public string AgentId => _agentId;
        public string AgentName => _agentName;



        public MockLombdaAgent(string agentId, string agentName)
        {
            _agentId = agentId;
            _agentName = agentName;
        }

        public bool IsTrue() { return true; }

        public override void InitializeAgent()
        {
            // Create a mock agent for testing
            ControlAgent = new Agent(
                new OpenAIModelClient("gpt-4o"),
                _agentName,
                $"You are a mock agent named {_agentName} for testing purposes."
            );
        }

        /// <summary>
        /// Override AddToConversation to avoid making real API calls
        /// </summary>
        public new Task<string> AddToConversation(string userInput, ModelItem message = null, bool streaming = true)
        {
            // If streaming is enabled, simulate streamed response
            if (streaming)
            {
                // Simulate a streamed response with a few parts
                var words = $"Hello! I'm {_agentName}, a mock agent. You said: {userInput}".Split(' ');

                Task.Run(async () =>
                {
                    foreach (var word in words)
                    {
                        // Emit each word as a separate streaming event
                        MainStreamingCallback?.Invoke(word + " ");
                        await Task.Delay(100);
                    }
                });
            }

            // Set thread ID if not already set
            if (string.IsNullOrEmpty(MainThreadId))
            {
                MainThreadId = Guid.NewGuid().ToString();
            }

            return Task.FromResult($"Hello! I'm {_agentName}, a mock agent. You said: {userInput}");
        }

    }

    

    /// <summary>
    /// Mock implementation of IModelClient for testing
    /// </summary>
    public class MockModelClient : ModelClient
    {
        public CancellationTokenSource CancelTokenSource { get; set; } = new();

        public Task<ModelResponse> _CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options = null)
        {
            // Create a mock response
            return Task.FromResult(new ModelResponse
            {
                Id = Guid.NewGuid().ToString(),
                OutputItems = new List<ModelItem>
                {
                    new ModelMessageItem(
                        Guid.NewGuid().ToString(), 
                        "ASSISTANT",
                        new List<ModelMessageContent>
                        {
                            new ModelMessageRequestTextContent("This is a mock response.")
                        },
                        ModelStatus.Completed
                    )
                }
            });
        }

        public Task<ModelResponse> _CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options = null, Runner.StreamingCallbacks streamingCallback = null)
        {
            // Simulate streaming by invoking the callback a few times
            if (streamingCallback != null)
            {
                Task.Run(async () =>
                {
                    var words = "This is a mock streaming response.".Split(' ');
                    foreach (var word in words)
                    {
                        streamingCallback(word + " ");
                        await Task.Delay(100);
                    }
                });
            }

            // Return the completed response
            return _CreateResponseAsync(messages, options);
        }
    }
}