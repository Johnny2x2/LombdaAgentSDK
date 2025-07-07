using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK;

namespace Examples.Basic
{
    internal class BasicHelloWorldStreaming
    {
        [Test]
        public async Task RunHelloWorldStreaming()
        {
            Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), "Assistant", "Have fun");

            RunResult result = await Runner.RunAsync(agent, "Hello Streaming World!", streaming:true, streamingCallback: Console.Write);
        }
    }
}
