using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.LombdaAgentExamples.ResearchAgentStateMachine
{
    class ResearchState : AgentState<WebSearchPlan, string>
    {
        public ResearchState(StateMachine stateMachine):base(stateMachine) { }
        public override Agent InitilizeStateAgent()
        {
            return new Agent(
            client: new LLMTornadoModelProvider(
                        model: ChatModel.OpenAi.Gpt41.V41Mini,
                        provider: [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],
                        enableWebSearch: true),

            _name: "Search agent",

            _instructions:"""
                    You are a research assistant. Given a search term, you search the web for that term and
                    produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                    words. Capture the main points. Write succinctly, no need to have complete sentences or good
                    grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                    essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                    """
            );
        }

        public override async Task<string> Invoke(WebSearchPlan plan)
        {
            return await InvokeThreaded(plan);
        }

        public async Task<string> InvokeThreaded(WebSearchPlan plan)
        {
            List<Task<string>> researchTask = new();
            plan.items.ToList()
                .ForEach(item =>
                    researchTask.Add(Task.Run(async () => await RunResearchAgent(item))));
            string[] researchResults = await Task.WhenAll(researchTask);
            return string.Join("[RESEARCH RESULT]\n\n\n", researchResults);
        }

        public async Task<string> RunResearchAgent(WebSearchItem item)
        {
            return await BeginRunnerAsync(InitilizeStateAgent(), item.query);
        }
    }
}
