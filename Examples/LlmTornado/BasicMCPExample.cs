using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.LlmTornado
{
    internal class BasicMCPExample
    {
        [Test]
        public async Task TestMCPMethodTool()
        {
            LLMTornadoModelProvider client = new(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);
            var mcpServer = new MCPServer("scriptwriter","C:\\Users\\johnl\\Downloads\\script-generation-mcp-main\\scriptwriter\\scriptwriter.py");
            Agent agent = new Agent(client,
                "Assistant",
                "You are a useful assistant.",
                _tools: [testFun],
                mcpServers: [mcpServer]
                );

            RunResult result = await Runner.RunAsync(agent, "What is the weather in MA?");

            Console.WriteLine(result.Text);
        }

        [Tool(Description ="Test Function Tool", In_parameters_description =["A simple test function tool that processes input and returns a string."])]
        public string testFun(string input)
        {
            Console.WriteLine($"Input received: {input}");
            return $"Processed input: {input}";
        }
    }
}
