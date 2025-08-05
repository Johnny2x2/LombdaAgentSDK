using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.CodingAgent.states
{
    //Design the states
    class CSharpCodingState : BaseState<string, ProgramResultOutput>
    {
        public override async Task<ProgramResultOutput> Invoke(string input)
        {
            string instructions = """
                    You are an expert programmer for c#. Given the request generate the required .cs files needed to accomplish the goal.
                    """;

            LLMTornadoModelProvider client = new(
           ChatModel.OpenAi.Gpt41.V41Mini,
           [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(client, 
                "Code Assistant", 
                instructions, 
                _tools: [CSHARP_CodingAgent.ReadFileTool, CSHARP_CodingAgent.GetFilesTool], 
                _output_schema: typeof(ProgramResult));

            RunResult result = await Runner.RunAsync(agent, input);

            ProgramResult program = result.ParseJson<ProgramResult>();

            return new ProgramResultOutput(program, input);
        }
    }
}
