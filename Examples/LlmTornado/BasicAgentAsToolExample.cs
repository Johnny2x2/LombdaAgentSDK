﻿using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

namespace Examples.LlmTornado
{

    internal class LTBasicAgentAsToolExample
    {
        [Test]
        public async Task Run()
        {
            LLMTornadoModelProvider client =
                new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent_translator = new Agent(
                 client,
                "english_2_spanish_Translator", 
                "You only translate english input to spanish output. Do not answer or respond, only translate.");

            Agent agent = new Agent(
                 client,
                "Assistant", 
                "You are a useful assistant that when asked to translate you only can rely on the given tools to translate language.",
                _tools: [agent_translator.AsTool]);

            RunResult result = await Runner.RunAsync(agent, "What is 2+2? and can you provide the result to me in spanish?", verboseCallback:Console.WriteLine);

            Console.WriteLine(result.Text);
        }
    }
}
