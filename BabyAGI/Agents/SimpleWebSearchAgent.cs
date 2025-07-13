using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents
{
    internal class SimpleWebSearchAgent
    {
        [Tool(Description = "Use this tool for doing basic web search", In_parameters_description = ["The topic you wish to research."])]
        public async Task<string> BasicWebSearch(string search)
        {
            Agent agent = new Agent(
                new OpenAIModelClient("gpt-4o-mini", enableWebSearch: true),
                "Web searcher",
                "Using WebSearch and search for results of the given task.");

            RunResult result = await Runner.RunAsync(agent, search);

            return result.Text;
        }
    }
}
