using BabyAGI.BabyAGIStateMachine.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.States
{
    public enum ProgressState
    {
        Progressing,
        Stagnating,
        Regression
    }

    public struct ProgressStatus
    {
        public ProgressState Status { get; set; }
        public string Evidence { get; set; }
        public string Suggestions { get; set; }
    }

    public class ProgressReport
    {
        public int StallCount { get; set; } = 0;
        public ProgressStatus Status { get; set; }

        public ProgressReport( ProgressStatus status, int stallCount)
        {
            Status = status;
            StallCount = stallCount;
        }

        public override string ToString()
        {
            return $"Stall Count: {StallCount}, Status: {Status.Status}, Evidence: {Status.Evidence}, Suggestions: {Status.Suggestions}";
        }
    }

    public class BAProgressManager : AgentState<QueueTask, ProgressReport>
    {
        public string CurrentScratchpad { get; set; } = string.Empty;
        public List<QueueTask> queueTasksForEval { get; set; } = new List<QueueTask>();
        public List<QueueTask> queueTasks { get; set; } = new List<QueueTask>();
        public int StallCount { get; set; } = 0;
        public BAProgressManager(StateMachine stateMachine) : base(stateMachine)
        {

        }

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""
            You are an AI agent responsible for monitoring the progress of an ongoing multi-step task. You have access to a list of completed sub-tasks, along with their individual results.

            Your goal is to determine if meaningful progress is being made toward achieving the overall objective, based on the outcomes of the completed sub-tasks.

            Instructions:
                Review the list of completed tasks and their results.

                Analyze how each result contributes toward the overall objective.

                Identify any patterns of success or repeated failure.

                Decide if there is clear progress toward the final goal, stagnation, or regression.

                If progress is insufficient, suggest possible reasons and next steps to address the lack of progress.
            """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Function Executor",
                instructions,
                _output_schema: typeof(TaskBreakdownResult));
        }

        public override async Task<ProgressReport> Invoke(QueueTask input)
        {
            string prompt = $@"
            User Goal: {input}

            Context: {CurrentScratchpad}
            ";

            var status = await BeginRunnerAsync<ProgressStatus>(prompt);
            if(status.Status == ProgressState.Stagnating || status.Status == ProgressState.Regression)
            {
                StallCount++;
            }
            else
            {
                StallCount = 0;
            }
            return new ProgressReport(status, StallCount);
        }

    }
}
