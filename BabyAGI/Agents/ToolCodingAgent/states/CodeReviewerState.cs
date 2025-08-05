using BabyAGI;
using BabyAGI.Utility;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.CodingAgent
{
    class CodeReviewerState : AgentState<CodeBuildInfoOutput, string>
    {
        public string ProjectName { get; set; } = string.Empty;
        public CodeReviewerState(StateMachine stateMachine) : base(stateMachine)
        {
        }

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.

                    Original Program Request was: 

                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return  new Agent(
                client,
                "CodeReviewer",
                instructions,
                _tools: [ReadFileTool, GetFilesTool],
                _output_schema: typeof(CodeReview));
        }

        public override async Task<string> Invoke(CodeBuildInfoOutput codeBuildInfo)
        {
            if (string.IsNullOrEmpty(ProjectName))
            {
                ProjectName = CurrentStateMachine.RuntimeProperties.TryGetValue("ProjectName", out object pName) ? pName.ToString() : string.Empty;
                if (string.IsNullOrEmpty(ProjectName))
                {
                    throw new InvalidOperationException("ProjectName cannot be empty");
                }
            }

            StateAgent.Instructions = $"""
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.

                    Original Program Request was: 
                    {codeBuildInfo.ProgramResult.ProgramRequest}
                    """;

            string Prompt = $"""
                    Build Errors: {codeBuildInfo.BuildInfo.BuildResult.Error}

                    Application Results: 
                    Input Args: {codeBuildInfo.ProgramResult.Result.Sample_EXE_Args}

                    Returned Result: {codeBuildInfo.BuildInfo.ExecutableResult.Output}

                    Error messages: {codeBuildInfo.BuildInfo.ExecutableResult.Error}
                    """;

            return (await BeginRunnerAsync<CodeReview>(Prompt)).ToString();
        }

        [Tool(Description = "Use this tool to read files already written", In_parameters_description = ["file path of the file you wish to read."])]
        public string ReadFileTool(string filePath)
        {
            return FunctionGeneratorUtility.ReadProjectFile(BabyAGIConfig.FunctionsPath, ProjectName, filePath);
        }

        [Tool(Description = "Use this tool to get all the file paths in the project")]
        public string GetFilesTool()
        {
            return FunctionGeneratorUtility.GetProjectFiles(BabyAGIConfig.FunctionsPath, ProjectName);
        }
    }

    
}
