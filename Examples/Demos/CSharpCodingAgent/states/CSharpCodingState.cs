using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
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
                    You are an expert programmer for c#. Given the request generate the required .cs files needed accomplish the goal.
                    """;

            Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), 
                "Assistant", 
                instructions, 
                _tools: [CSHARP_CodingAgent.ReadFileTool, CSHARP_CodingAgent.GetFilesTool], 
                _output_schema: typeof(ProgramResult));

            RunResult result = await Runner.RunAsync(agent, input);

            ProgramResult program = result.ParseJson<ProgramResult>();

            return new ProgramResultOutput(program, input);
        }
    }
}
