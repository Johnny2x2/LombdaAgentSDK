using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using Examples.Demos.ProjectCodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.ProjectCodingAgent.states
{
    [Description("Output a list of command line args for the exe execution to test the project (The system will handle adding the exe to the command line so don't include that)")]
    public struct ProjectInputs
    {
        [Description("args to run into the exe to test the project")]
        public CommandLineArgs[] commandline_argument_examples { get; set; }
    }

    public class ProjectApprovalState : BaseState<ProgramDesignResult, ProgramApprovalResult>
    {
        public CodingProjectsAgent StateAgent { get; set; }

        public ProjectApprovalState(CodingProjectsAgent stateAgent)
        {
            StateAgent = stateAgent;
        }

        public override async Task<ProgramApprovalResult> Invoke(ProgramDesignResult input)
        {
            
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            string prompt = $@"
            Project Goal: {input.Request}

            Program Design Description: {input.Design.Description}

            Input Args: {input.Design.ExpectedInputArgs}

            Expected Results: {input.Design.ExpectedResult}

            Success Criteria: {input.Design.SuccessCriteria}
            ";

            string getterInstructions = $"""
                    You are an expert c# program input getter. Given the request, design a exe input args that can be used to test the program for the given request.
                    """;

            Agent inputAgent = new Agent(
                client,
                "inputgetter",
                getterInstructions,
                _tools: [StateAgent.ReadFileTool, StateAgent.GetFilesTool, TestExeProgram],
                _output_schema: typeof(ProjectInputs));

            RunResult result = await Runner.RunAsync(inputAgent, prompt);

            ProjectInputs inpts = result.ParseJson< ProjectInputs >();
            if (inpts.commandline_argument_examples == null || inpts.commandline_argument_examples.Length == 0)
            {
                throw new Exception("No command line args were generated for the project. Please check the input getter agent.");
            }

            ConcurrentBag<(string,ExecutableOutputResult)> exeResult = new();
            foreach (CommandLineArgs args in inpts.commandline_argument_examples)
            {
                exeResult.Add((args.input_args_array, await FunctionGeneratorUtility.FindAndRunExecutableAndCaptureOutput(StateAgent.FunctionsPath, StateAgent.ProjectName, "net8.0", args.input_args_array)));
            }

            string exeResults = string.Join("\n\n", exeResult.Select(r => 
            r.Item2.ExecutionCompleted? 
                $"Successful Input args {r.Item1} resulted in => {r.Item2.Output}" :
                $"Failed Input args {r.Item1} resulted in => {r.Item2.Error}")); 

            string approvalInstructions = $"""
                    You are an expert programmer approver for C#. Given the generated C# project, review the design, code, output results to determine if it meets the user's request.
                    Original Program Request was: 
                    {input.Request}
                    """;
            Agent approvalAgent = new Agent(
                client,
                "approver",
                approvalInstructions,
                _tools: [StateAgent.ReadFileTool, StateAgent.GetFilesTool, TestExeProgram],
                _output_schema: typeof(ProgramApproval));

            RunResult approvalResult = await Runner.RunAsync(approvalAgent, $"Output Results: {exeResults}");

            ProgramApproval approval = approvalResult.ParseJson<ProgramApproval>();

            return new ProgramApprovalResult() { 
                Approval = approval
            };
        }

        [Tool(Description = "Runs the generated program with the provided arguments and captures the output.", In_parameters_description = ["exe parameter args for program"] )]
        public async Task<ExecutableOutputResult> TestExeProgram(string args)
        {
            return await FunctionGeneratorUtility.FindAndRunExecutableAndCaptureOutput(StateAgent.FunctionsPath, StateAgent.ProjectName, "net8.0", args);
        }

    }
}
