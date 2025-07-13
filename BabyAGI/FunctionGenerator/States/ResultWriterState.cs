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
    public struct FinalResult
    {
        public string AssistantMessage { get; set; }
        public string FinalResultSummary { get; set; }
    }

    public class ResultWriterState : BaseState<FunctionResultReviewOutput, FinalResult>
    {
        public FunctionGeneratorAgent StateController { get; set; }
        public ResultWriterState(FunctionGeneratorAgent stateController) { StateController = stateController; }

        public override async Task<FinalResult> Invoke(FunctionResultReviewOutput input)
        {
            string prompt =
                   $@"User Request:
                   {StateController.OriginalTask}

                    review summary: 
                    {input.functionResultReview.InformationSummary}

                    Collected Result: 
                    {string.Join("\n\n", StateController.SavedResults)}
                    ";

            string instructions = $"""
                    You are an expert Report Writer who has collected results that were deemed to contain enough information to answer the users question.  
                    Please write a reponse to the users question to the best of your ability.
                    """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "Result Writer",
                instructions,
                _output_schema: typeof(FinalResult));

            RunResult result = await Runner.RunAsync(agent, prompt);

            FinalResult response = result.ParseJson<FinalResult>();

            return response;
        }
    }
}
