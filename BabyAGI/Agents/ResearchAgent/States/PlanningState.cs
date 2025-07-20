using BabyAGI.Agents.ResearchAgent.DataModels;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.AgentStateSystem;

namespace BabyAGI.Agents.ResearchAgent.States
{
    //Design the states
    class PlanningState : AgentState<string, WebSearchPlan>
    {
        int attempts = 0;
        int maxAttempts = 5;

        public PlanningState(StateMachine stateMachine) : base(stateMachine)
        {
        }

        public async override Task EnterState(string? input)
        {
            attempts = 0; //Reset attempts if state was exited/ReEntered (StateMachine will just case Invoke if State Doesn't change)
        }

        public override Agent InitilizeStateAgent()
        {
            LLMTornadoModelProvider client = new(
                        ChatModel.OpenAi.Gpt41.V41Mini,
                        [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            string instructions = """
                    You are a helpful research assistant. Given a query, come up with a set of web searches, 
                    to perform to best answer the query. Output between 5 and 20 terms to query for. 
                    """;

            return new Agent(client, "Assistant", instructions, _output_schema: typeof(WebSearchPlan));
        }

        public override async Task<WebSearchPlan> Invoke(string input)
        {
            attempts++;
            if (attempts > maxAttempts)
            {
                CurrentStateMachine.Stop(); //Kill the process on max attempts
                throw new Exception("Max plan generations reached");
            }

            return await BeginRunnerAsync<WebSearchPlan>(input);
        }
    }
}
