using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;

namespace Examples.Basic
{
    internal class LTBasicHelloWorldStreaming
    {
        [Test]
        public async Task RunHelloWorldStreaming()
        {
            LLMTornadoModelProvider client =
              new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(client, "Assistant", "Have fun");

            RunResult result = await Runner.RunAsync(agent, "Hello Streaming World!", streaming:true, streamingCallback: Console.Write);
        }
    }
}
