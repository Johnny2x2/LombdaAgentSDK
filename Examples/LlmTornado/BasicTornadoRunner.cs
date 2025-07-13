using LlmTornado.Chat.Models;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK;
using LlmTornado.Code;

namespace Examples.LlmTornado
{
    public class BasicTornadoRunner
    {
        [Test]
        public async Task BasicTornadoRun()
        {
            LLMTornadoModelProvider client = new(
            ChatModel.OpenAi.Gpt41.V41Mini,
            [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(client, "Assistant", "You are a useful assistant.");

            RunResult result = await Runner.RunAsync(agent, "what is 2+2");

            Console.WriteLine(result.Text);
        }
    }
}
