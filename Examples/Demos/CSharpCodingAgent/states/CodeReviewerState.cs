using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LombdaAgentSDK.Agents.Tools;

namespace Examples.Demos.CodingAgent
{
    class CodeReviewerState : BaseState<CodeBuildInfoOutput, string>
    {
        public override async Task<string> Invoke(CodeBuildInfoOutput codeBuildInfo)
        {
            string instructions = $"""
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.

                    Original Program Request was: 

                    {codeBuildInfo.ProgramResult.ProgramRequest}
                    """;

            Agent agent = new Agent(
                new OpenAIModelClient("gpt-4o-mini"),
                "CodeReviewer",
                instructions,
                _tools: [CSHARP_CodingAgent.ReadFileTool, CSHARP_CodingAgent.GetFilesTool],
                _output_schema: typeof(CodeReview));

            RunResult result = await Runner.RunAsync(agent, $"Errors Generated {codeBuildInfo.BuildInfo.BuildResult.Error}");

            CodeReview review = result.ParseJson<CodeReview>();

            return review.ToString();
        }

    }

    
}
