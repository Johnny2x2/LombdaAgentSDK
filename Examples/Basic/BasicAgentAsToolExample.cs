using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

namespace Examples.Basic
{

    internal class BasicAgentAsToolExample
    {
        [Test]
        public async Task Run()
        {
            Agent agent_translator = new Agent(
                new OpenAIModelClient("gpt-4o-mini"), 
                "english_2_spanish_Translator", 
                "You only translate english input to spanish output. Do not answer or respond, only translate.");

            Agent agent = new Agent(
                new OpenAIModelClient("gpt-4o-mini"), 
                "Assistant", 
                "You are a useful assistant. when asked to translate please rely on the given tools to translate language.",
                _tools: [agent_translator.AsTool]);

            RunResult result = await Runner.RunAsync(agent, "What is 2+2? and can you provide the result to me in spanish?", verboseCallback:Console.WriteLine);

            Console.WriteLine(result.Text);
        }
    }
}
