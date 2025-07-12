using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.CodingAgent
{
    class CodeReviewerState : BaseState<CodeBuildInfoOutput, string>
    {
        public CSHARP_CodingAgent StateAgent {  get; set; }
        public CodeReviewerState(CSHARP_CodingAgent stateAgent)
        {
            StateAgent = stateAgent;
        }

        public override async Task<string> Invoke(CodeBuildInfoOutput codeBuildInfo)
        {
            string instructions = $"""
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.

                    Original Program Request was: 

                    {codeBuildInfo.ProgramResult.ProgramRequest}
                    """;

            LLMTornadoModelProvider client = new(   ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "CodeReviewer",
                instructions,
                _tools: [StateAgent.ReadFileTool, StateAgent.GetFilesTool],
                _output_schema: typeof(CodeReview));

            string Prompt = $"""
                    Build Errors: {codeBuildInfo.BuildInfo.BuildResult.Error}

                    Application Results: 
                    Input Args: {codeBuildInfo.ProgramResult.Result.Sample_EXE_Args}

                    Returned Result: {codeBuildInfo.BuildInfo.ExecutableResult.Output}

                    Error messages: {codeBuildInfo.BuildInfo.ExecutableResult.Error}
                    """;

            RunResult result = await Runner.RunAsync(agent, $"Errors Generated {codeBuildInfo.BuildInfo.BuildResult.Error}");

            CodeReview review = result.ParseJson<CodeReview>();

            return review.ToString();
        }

    }

    
}
