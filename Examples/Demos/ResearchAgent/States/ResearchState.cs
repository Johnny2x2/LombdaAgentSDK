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

namespace Examples.Demos.ResearchAgent.States
{
    class ResearchState : BaseState<WebSearchPlan, string>
    {
        public override async Task<string> Invoke()
        {
            string instructions = """
                    You are a research assistant. Given a search term, you search the web for that term and
                    produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                    words. Capture the main points. Write succinctly, no need to have complete sentences or good
                    grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                    essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                    """;

            Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini", enableWebSearch: true), "Search agent", instructions);

            List<string> searchResults = new List<string>();

            Console.WriteLine("[Starting WebSearch from StateMachine]");

            foreach (WebSearchItem item in this.Input.items)
            {
                RunResult result = await Runner.RunAsync(agent, item.query);

                searchResults.Add(result.Text ?? "");
            }

            return string.Join("[SEARCH RESULT]\n\n\n", searchResults);
        }
    }
}
