using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using Examples.Demos.FunctionGenerator;
using Examples.Demos.FunctionGenerator.States;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public class FunctionExecutionResult
    {
        public ExecutableOutputResult ExecutionResult { get; set; }
        public FunctionFoundResultOutput functionFoundResultOutput { get; set; }

        public CommandLineArgs generatedArgs { get; set; }
        public FunctionExecutionResult(ExecutableOutputResult executionResult, FunctionFoundResultOutput functionFoundResultOutput, CommandLineArgs generatedArgs)
        {
            ExecutionResult = executionResult;
            this.functionFoundResultOutput = functionFoundResultOutput;
            this.generatedArgs = generatedArgs;
        }
    }
    public class FunctionExecutorState : BaseState<FunctionFoundResultOutput, FunctionExecutionResult>
    {
        public FunctionGeneratorAgent StateController { get; set; }
        public FunctionExecutorState(FunctionGeneratorAgent stateController) { StateController = stateController; }
        public string PreviousErrors { get; set; } = "";

        public override async Task<FunctionExecutionResult> Invoke(FunctionFoundResultOutput input)
        {
            string prompt =
                    $@"User Request:
                        {input.UserInput}
                        
                       Function To Execute: {input.FoundResult.FunctionName}  

                       Function Description:
                        {FunctionGeneratorUtility.ReadProjectDescription(StateController.FunctionsPath, input.FoundResult.FunctionName)}

                       Errors To Correct From Last run:
                        {PreviousErrors}

                       Sample Working Args on file: 
                        {FunctionGeneratorUtility.ReadProjectArgs(StateController.FunctionsPath, input.FoundResult.FunctionName)}
                    ";

            string instructions = $"""
                    You are an expert programmer for c#. Your task is to generate input args to run for the following EXE execution given by the user prompt.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "Function Executor",
                instructions,
                _output_schema: typeof(CommandLineArgs));

            RunResult result = await Runner.RunAsync(agent, prompt);

            CommandLineArgs lineArgs = result.ParseJson<CommandLineArgs>();

            ExecutableOutputResult outputResult = await FunctionGeneratorUtility.FindAndRunExecutableAndCaptureOutput(StateController.FunctionsPath, input.FoundResult.FunctionName, "net8.0", lineArgs.input_args_array);

            PreviousErrors = outputResult.ExecutionCompleted ? "" : outputResult.Error;

            return new FunctionExecutionResult(outputResult, input, lineArgs);
        }
    }
}
