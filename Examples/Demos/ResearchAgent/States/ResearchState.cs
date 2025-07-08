using Examples.Demos.ResearchAgent.DataModels;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LlmTornado.Responses;

namespace Examples.Demos.ResearchAgent.States
{
    class ResearchState : BaseState<WebSearchPlan, string>
    {
        bool RunParallel { get; set; } = false;
        public ResearchState(bool runParallel = false) { RunParallel = runParallel; }

        public override async Task<string> Invoke()
        {
            Console.WriteLine("[Starting WebSearch from StateMachine]");

            return RunParallel ? await InvokeParallel() : await InvokeThreaded();
        }

        public async Task<RunResult> RunResearchAgent(WebSearchItem item)
        {
            string instructions = """
                    You are a research assistant. Given a search term, you search the web for that term and
                    produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                    words. Capture the main points. Write succinctly, no need to have complete sentences or good
                    grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                    essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                    """;
            Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini", enableWebSearch: true), "Search agent", instructions);
            return await Runner.RunAsync(agent, item.query);
        }

        public async Task<string> InvokeParallel()
        {
            ConcurrentBag<RunResult> researchResults = new ConcurrentBag<RunResult>();

            await Parallel.ForEachAsync(
                Input.items.AsEnumerable(),
                async (item, CancellationToken) => researchResults.Add((await RunResearchAgent(item))));

            return string.Join("[RESEARCH RESULT]\n\n\n", researchResults.ToList().Select(result => result.Text));
        }

        public async Task<string> InvokeThreaded()
        {
            List<Task<RunResult>> researchTask = new List<Task<RunResult>>();
            
            Input.items.ToList()
                .ForEach(item =>
                    researchTask.Add(Task.Run(async () => await RunResearchAgent(item))));

            RunResult[] researchResults = await Task.WhenAll(researchTask);

            return string.Join("[RESEARCH RESULT]\n\n\n", researchResults.ToList().Select(result => result.Text));
        }
    }
}
