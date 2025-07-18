using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.FunctionGenerator.DataModels;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using Examples.Demos.FunctionGenerator;
using Examples.Demos.FunctionGenerator.States;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public class FunctionExecutorState : AgentState<FunctionFoundResultOutput, FunctionExecutionResult>
    {
        string functionsPath = "";
        public FunctionExecutorState(StateMachine stateMachine):base(stateMachine) {  }
        public string PreviousErrors { get; set; } = "";

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""
                    You are an expert programmer for c#. Your task is to generate input args to run for the following EXE execution given by the user prompt.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Function Executor",
                instructions,
                _output_schema: typeof(CommandLineArgs));
        }

        public override async Task<FunctionExecutionResult> Invoke(FunctionFoundResultOutput input)
        {
            if (string.IsNullOrEmpty(functionsPath))
            {
                functionsPath = CurrentStateMachine.RuntimeProperties.TryGetValue("FunctionsPath", out object? path) ? path?.ToString() : null;
                if (string.IsNullOrEmpty(functionsPath))
                    throw new InvalidOperationException("FunctionsPath is not set in the runtime properties.");
            }

            string prompt =
                    $@"User Request:
                        {input.UserInput}
                        
                       Function To Execute: {input.FoundResult.FunctionName}  

                       Function Description:
                        {FunctionGeneratorUtility.ReadProjectDescription(functionsPath, input.FoundResult.FunctionName)}

                       Errors To Correct From Last run:
                        {PreviousErrors}

                       Sample Working Args on file: 
                        {FunctionGeneratorUtility.ReadProjectArgs(functionsPath, input.FoundResult.FunctionName)}
                    ";

            CommandLineArgs lineArgs = await BeginRunnerAsync<CommandLineArgs>(prompt);

            ExecutableOutputResult outputResult = await FunctionGeneratorUtility.FindAndRunExecutableAndCaptureOutput(functionsPath, input.FoundResult.FunctionName, "net8.0", lineArgs.input_args_array);

            PreviousErrors = outputResult.ExecutionCompleted ? "" : outputResult.Error;

            return new FunctionExecutionResult(outputResult, input, lineArgs);
        }
    }
}
