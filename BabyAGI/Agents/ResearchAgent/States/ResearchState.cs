using BabyAGI.Agents.ResearchAgent.DataModels;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LlmTornado.Responses;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;

namespace BabyAGI.Agents.ResearchAgent.States
{
    class ResearchState : AgentState<WebSearchPlan, string>
    {
        public ResearchState(StateMachine stateMachine, bool runParallel = false):base(stateMachine) { }

        public override async Task<string> Invoke(WebSearchPlan plan)
        {
            Console.WriteLine("[Starting WebSearch from StateMachine]");

            return await InvokeThreaded(plan);
        }

        public async Task<string> RunResearchAgent(WebSearchItem item)
        {
            //Using the manual runner to run the agent
            return await BeginRunnerAsync(InitilizeStateAgent(), item.query);
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

        
        public override Agent InitilizeStateAgent()
        {
            string instructions = """
                    You are a research assistant. Given a search term, you search the web for that term and
                    produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                    words. Capture the main points. Write succinctly, no need to have complete sentences or good
                    grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                    essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                    """;

            return new Agent(new OpenAIModelClient("gpt-4o-mini", enableWebSearch: true), "Search agent", instructions);
        }
    }
}
