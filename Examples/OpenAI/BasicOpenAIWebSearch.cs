using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.OpenAI
{
    public class BasicOpenAIWebSearch
    {
        [Test]
        public async Task Run()
        {
            Agent agent = new Agent(
                new OpenAIModelClient("gpt-4o-mini", enableWebSearch: true),
                "Assistant",
                "Have fun");

            RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");

            Console.WriteLine($"[ASSISTANT]: {result.Text}");
        }
    }
}
