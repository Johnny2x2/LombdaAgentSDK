using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Utility;
using Examples.Demos.FunctionGenerator;
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

namespace BabyAGI.FunctionGenerator.States
{
    public struct FunctionResultReview
    {
        public bool HasEnoughInformation { get; set; } 
        public string InformationSummary { get; set; } 
    }

    public class FunctionResultReviewOutput
    {
        public FunctionResultReview functionResultReview { get; set; }
        public FunctionExecutionResult functionExecutionResult { get; set; }
        public FunctionResultReviewOutput() { }
        public FunctionResultReviewOutput(FunctionExecutionResult functionResult, FunctionResultReview review) 
        {
            functionResultReview = review;
            functionExecutionResult = functionResult;
        }
    }
    internal class ResultReviewerState : BaseState<FunctionExecutionResult, FunctionResultReviewOutput>
    {
        public FunctionGeneratorAgent StateController { get; set; }
        public ResultReviewerState(FunctionGeneratorAgent stateController) { StateController = stateController; }

        
        FunctionResultReview lastSummary = new();

        public override async Task<FunctionResultReviewOutput> Invoke(FunctionExecutionResult input)
        {
            if (string.IsNullOrEmpty(lastSummary.InformationSummary))
            {
                lastSummary.InformationSummary = "";
            }
            
            StateController.SavedResults.Add($"Function: [{input.functionFoundResultOutput.FoundResult.FunctionName}]\nWith Input Args: ({input.generatedArgs})\n Result: {input.ExecutionResult.Output}");

            string prompt =
                    $@"
                     User Request:
                     {StateController.OriginalTask}
                       
                    lastReview:
                    {lastSummary.InformationSummary}

                    Collected Result: {string.Join("\n\n", StateController.SavedResults)}
                    ";

            string instructions = $"""
                    You are an expert reviewer collecting results. Do your best to determine if there is enough information to generate a final report or if more information is required to generate a report.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "Result Reviewer",
                instructions,
                _output_schema: typeof(FunctionResultReview));

            RunResult result = await Runner.RunAsync(agent, prompt);

            FunctionResultReview review = result.ParseJson<FunctionResultReview>();

            lastSummary = review;

            return new FunctionResultReviewOutput(input, review);
        }

        public override async Task ExitState()
        {
            if(lastSummary.HasEnoughInformation)   
            {
                lastSummary = new();
            }
        }
    }
}
