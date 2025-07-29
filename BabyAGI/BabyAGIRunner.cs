using BabyAGI.Agents;
using BabyAGI.Agents.ResearchAgent;
using BabyAGI.Agents.ResearchAgent.DataModels;
using BabyAGI.BabyAGIStateMachine;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.AgentStateSystem;
using System.Threading.Tasks;

namespace BabyAGI
{
    public class BabyAGIRunner : LombdaAgent
    {
        /// <summary>
        /// Setup the input preprocessor to run the state machine
        /// </summary>
        public BabyAgiStateMachine _stateMachine { get; set; } 

        public BabyAGIRunner(string name) : base(name) 
        {             //Initialize the agent
            _stateMachine = new BabyAgiStateMachine(this);
            InputPreprocessor = RunStateMachine;
        }

        public override void InitializeAgent()
        {
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], useResponseAPI:true);
            string instructions = $"""You are a person assistant AGI.""";
            ControlAgent = new Agent(client, "BabyAGI", instructions);
        }

        public async Task<string> RunStateMachine(string task)
        {
            //Return the final result
            var result = (await _stateMachine.Run(task))[0].Status.UpdatedProgressSummary ?? task;
            return result.ToString() ?? task;
        }
    }

}
