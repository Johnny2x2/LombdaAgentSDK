using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK;

namespace Examples.Basic
{
    internal class BasicHelloWorld
    {
        [Test]
        public async Task RunHelloWorld()
        {
            Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), "Assistant", "Have fun");

            RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");

            Console.WriteLine($"[ASSISTANT]: {result.Text}");
        }
    }
}
