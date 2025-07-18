using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.FunctionGenerator.DataModels;
using BabyAGI.Utility;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    internal class ResultReviewerState : AgentState<FunctionExecutionResult, FunctionResultReviewOutput>
    {
        public List<string> SavedResults = new List<string>();
        public string OriginalTask = "";
        public ResultReviewerState(StateMachine stateMachine) : base(stateMachine) {}
        
        FunctionResultReview lastSummary = new();

        public override async Task<FunctionResultReviewOutput> Invoke(FunctionExecutionResult input)
        {
            if (string.IsNullOrEmpty(lastSummary.InformationSummary))
            {
                lastSummary.InformationSummary = "";
            }

            string newReslt = $"Function: [{input.functionFoundResultOutput.FoundResult.FunctionName}]\nWith Input Args: ({input.generatedArgs})\n Result: {input.ExecutionResult.Output}";

            if (string.IsNullOrEmpty(OriginalTask))
            {
                OriginalTask = CurrentStateMachine.RuntimeProperties.TryGetValue("OrginalTask", out object orginalTask) ? orginalTask.ToString() : string.Empty;
                if (string.IsNullOrEmpty(OriginalTask))
                {
                    throw new InvalidOperationException("Original task is not set in the runtime properties.");
                }
            }

            SavedResults.Add(newReslt);

            CurrentStateMachine.RuntimeProperties.AddOrUpdate("SavedResults", new List<string> { newReslt }, (existingKey, existingValue) => // Update logic if key exists
            {
                if(existingValue is List<string> existingValues)
                {
                    existingValues.Add(newReslt);
                    return existingValues;
                }
                else
                {
                    throw new InvalidOperationException("SavedResults should always be a List<string>.");
                }
            });


            string prompt =
                    $@"
                     User Request:
                     {OriginalTask}
                       
                    lastReview:
                    {lastSummary.InformationSummary}

                    Collected Result: {string.Join("\n\n", SavedResults)}
                    ";

            FunctionResultReview review = await BeginRunnerAsync<FunctionResultReview>( prompt);

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

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""
                    You are an expert reviewer collecting results. Do your best to determine if there is enough information to generate a final report or if more information is required to generate a report.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Result Reviewer",
                instructions,
                _output_schema: typeof(FunctionResultReview));
        }
    }
}
