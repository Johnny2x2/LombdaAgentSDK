using BabyAGI.FunctionGenerator.DataModels;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public class ResultWriterState : AgentState<FunctionResultReviewOutput, FinalResult>
    {
        string OriginalTask = "";
        List<string> SavedResults = new List<string>();
        public ResultWriterState(StateMachine stateMachine) : base(stateMachine) {}

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""
                    You are an expert Report Writer who has collected results that were deemed to contain enough information to answer the users question.  
                    Please write a reponse to the users question to the best of your ability.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Result Writer",
                instructions,
                _output_schema: typeof(FinalResult));
        }

        public override async Task<FinalResult> Invoke(FunctionResultReviewOutput input)
        {
            if (string.IsNullOrEmpty(OriginalTask))
            {
                OriginalTask = CurrentStateMachine.RuntimeProperties.TryGetValue("OrginalTask", out object orginalTask) ? orginalTask.ToString() : string.Empty;
                if (string.IsNullOrEmpty(OriginalTask))
                {
                    throw new InvalidOperationException("Original task is not set in the runtime properties.");
                }
            }

            if(CurrentStateMachine.RuntimeProperties.TryGetValue("SavedResults", out object savedResultsObj))
            {
                if (savedResultsObj is List<string> savedResults)
                {
                    SavedResults = savedResults;
                }
                else
                {
                    throw new InvalidOperationException("SavedResults should always be a List<string>.");
                }
            }

            string prompt =
                $@"User Request:
                {OriginalTask}

                review summary: 
                {input.functionResultReview.InformationSummary}

                Collected Result: 
                {string.Join("\n\n", SavedResults)}
                ";

            FinalResult response = await BeginRunnerAsync<FinalResult>( prompt);

            return response;
        }
    }
}
