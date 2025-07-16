using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.BabyAGIStateMachine.DataModels;
using Examples.Demos.CodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.ProjectCodingAgent.states
{
    //Design the states
    class ProjectCodingState : BaseState<TaskItem, ProjectResultOutput>
    {
        public CodingProjectsAgent StateAgent { get; set; }

        public ProjectCodingState(CodingProjectsAgent stateAgent) 
        { 
            StateAgent = stateAgent;
        }

        public override async Task<ProjectResultOutput> Invoke(TaskItem input)
        {
            string prompt = $@"""
You are an expert C# programmer. Your task is to write detailed and working code for the following task based on the context provided. 
The C# Console Program will be Built and executed later from a .exe and will use the input args to get the project to work.
Make sure Program code is the main entry to utilize .net8.0 args structure when executing exe
Do not provide placeholder code, but rather do your best like you are the best senior engineer in the world and provide the best code possible. DO NOT PROVIDE PLACEHOLDER CODE.

Current Project Name: {StateAgent.ProjectName}

Overall context:
{input.Description}

Expected Outcome:
{input.ExpectedOutcome}

Success Criteria:
{input.SuccessCriteria}

Task Complexity: {input.Complexity} 
""";

            LLMTornadoModelProvider client = new(
           ChatModel.OpenAi.Gpt41.V41Mini,
           [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(client, 
                "Code Assistant",
                "You are an expert C# programmer. Your task is to write detailed and working code for the following project based on the context provided.",
                _output_schema: typeof(ProgramResult));

            RunResult result = await Runner.RunAsync(agent, prompt);

            ProgramResult program = result.ParseJson<ProgramResult>();

            return new ProjectResultOutput(program, input);
        }
    }
}
