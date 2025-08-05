using LombdaAgentSDK.Agents;
using LombdaAgentSDK;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;

namespace Examples.LombdaAgentExamples.ResearchAgentStateMachine
{
    //Design the states
    class PlanningState : AgentState<string, WebSearchPlan>
    {
        public PlanningState(StateMachine stateMachine) : base(stateMachine) { }

        public override Agent InitilizeStateAgent()
        {
            return new Agent(
                client: new LLMTornadoModelProvider(
                        ChatModel.OpenAi.Gpt41.V41Mini,
                        [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]), 
                _name: "Assistant", 
                _instructions: """
                    You are a helpful research assistant. Given a query, come up with a set of web searches, 
                    to perform to best answer the query. Output between 5 and 20 terms to query for. 
                    """, 
                _output_schema: typeof(WebSearchPlan));
        }

        public override async Task<WebSearchPlan> Invoke(string input)
        {
            return await BeginRunnerAsync<WebSearchPlan>(input);
        }
    }
}
