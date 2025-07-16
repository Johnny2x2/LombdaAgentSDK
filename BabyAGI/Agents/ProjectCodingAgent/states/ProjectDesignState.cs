using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.Utility;
using Examples.Demos.ProjectCodingAgent;
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

namespace BabyAGI.Agents.ProjectCodingAgent.states
{
    internal class ProjectDesignState : BaseState<string, ProgramDesignResult>
    {
        public CodingProjectsAgent StateAgent { get; set; }

        public ProjectDesignState(CodingProjectsAgent stateAgent)
        {
            StateAgent = stateAgent;
        }

        public override async Task<ProgramDesignResult> Invoke(string request)
        {
            string instructions = $"""
                    You are an expert program designer for C#. Given the request, design a C# program that stasfies the usesr's request. 
                    Define the program's Input Args, Expected Outcome, and Success Criteria for completion.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "Code Designer",
                instructions,
                _output_schema: typeof(ProgramDesign));

            string Prompt = $"""
                    My program design request is for: {request}
                    """;

            RunResult result = await Runner.RunAsync(agent, Prompt);

            ProgramDesign design = result.ParseJson<ProgramDesign>();

            if(string.IsNullOrEmpty(StateAgent.ProjectName))
            {
                throw new Exception("Project name cannot be empty. Please provide a valid project name.");
            }

            StateAgent.ProjectName = CreateNewProject(StateAgent.FunctionsPath, design.ProjectName, design.Description);

            return new ProgramDesignResult() { Design = design, Request = request };
        }

        //Check if directory exists, if is does then make a (ProjectName)_1 and check if that that exists, if it does then make a (ProjectName)_2 and so on until it finds a directory that does not exist
        private string CreateNewProject(string functionsPath, string projectName, string description)
        {
            string projectPath = Path.Combine(functionsPath, projectName);
            int counter = 1;
            while (Directory.Exists(projectPath))
            {
                projectPath = Path.Combine(functionsPath, $"{projectName}_{counter}");
                counter++;
            }
            FunctionGeneratorUtility.CreateNewProject(functionsPath, projectName, description);
            // Optionally, create a README or other initial files
            File.WriteAllText(Path.Combine(projectPath, "README.md"), $"# {projectName}\n\n{description}");
            return projectName;
        }

    }
}
