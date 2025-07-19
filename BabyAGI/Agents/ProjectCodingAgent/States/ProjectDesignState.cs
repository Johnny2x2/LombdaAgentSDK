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
    internal class ProjectDesignState : AgentState<string, ProgramDesignResult>
    {
        public CodingProjectsAgent StateAgent { get; set; }

        public ProjectDesignState(CodingProjectsAgent stateAgent)
        {
            StateAgent = stateAgent;
        }

        public override async Task<ProgramDesignResult> Invoke(string request)
        {

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            string instructions = $"""
                    You are an expert program designer for C# console applications. Given the request, design a C# console program that stasfies the usesr's request. 
                    Define the program's Input Args, Expected Outcome, and Success Criteria for completion.
                    """;

            Agent designAgent = new Agent(
                client,
                "Code Designer",
                instructions,
                _output_schema: typeof(ProgramDesign));

            string editInstructions = $"""
                    You are an expert program designer for C# console applications. Given the request, edit the existing C# console program that stasfies the usesr's request. 
                    Define the program's Input Args, Expected Outcome, and Success Criteria for completion.
                    """;

            Agent editAgent = new Agent(
                client,
                "Code Editor",
                instructions,
                _tools: [StateAgent.ReadFileTool, StateAgent.GetFilesTool],
                _output_schema: typeof(ProgramDesign));

            Agent agent = StateAgent.InEditMode ? editAgent : designAgent;

            string Prompt = $"""
                    My program design request is for: {request}
                    """;

            RunResult result = await Runner.RunAsync(agent, Prompt);

            ProgramDesign design = result.ParseJson<ProgramDesign>();

            if(StateAgent.InEditMode)
            {
                design.ProjectName = StateAgent.ProjectName;
            }
            else
            {
                StateAgent.ProjectName = CreateNewProject(StateAgent.FunctionsPath, design.ProjectName, design.Description);
            }
            
            StateAgent.ProjectRequirements = new ProgramDesignResult() { Design = design, Request = request };

            if (string.IsNullOrEmpty(StateAgent.ProjectName))
            {
                throw new Exception("Project name cannot be empty. Please provide a valid project name.");
            }

            return StateAgent.ProjectRequirements;
        }

        //Check if directory exists, if is does then make a (ProjectName)_1 and check if that that exists, if it does then make a (ProjectName)_2 and so on until it finds a directory that does not exist
        private string CreateNewProject(string functionsPath, string projectName, string description)
        {
            string projectNewName = projectName; 
            int counter = 1;

            while (Directory.Exists(Path.Combine(functionsPath, projectNewName)))
            {
                projectNewName = $"{projectNewName}_{counter}";
                counter++;
            }

            if(!FunctionGeneratorUtility.CreateNewProject(functionsPath, projectNewName, description))
            {
                throw new Exception($"Failed to create project directory at {Path.Combine(functionsPath, projectNewName)}. Please check permissions or path validity.");
            }
            
            
            return projectNewName;
        }

    }
}
