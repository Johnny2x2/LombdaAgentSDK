using BabyAGI.BabyAGIStateMachine.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.States
{
    public enum ProgressState
    {
        Completed,
        Progressing,
        Stagnating,
        Regression
    }

    [Description("Progress report on current goal, Status can only be: Completed,\r\n        Progressing,\r\n        Stagnating,\r\n        Regression")]
    public struct ProgressStatus
    {
        public string UpdatedProgressSummary { get; set; } 
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
                _output_schema: typeof(ProgressStatus));
        }

        public override async Task<ProgressReport> Invoke(QueueTask input)
        {
            var goal = CurrentStateMachine.RuntimeProperties.TryGetValue("UserGoal", out object goalObj) ? goalObj as string : input.Task;
            queueTasksForEval = CurrentStateMachine.RuntimeProperties.TryGetValue("queueTasksForEval", out object evalQueue) 
                ? (List<QueueTask>)evalQueue 
                : new List<QueueTask>();
            string prompt = $@"
            User Goal: {goal}
    
            Last Task: {input.Task}

           Last Result: {input.Result}

            Current Tasks for Evaluation: {string.Join("\n", queueTasksForEval.Select(t => $"{t.Task} - Result: {t.Result}"))}

            Current Summary: {CurrentScratchpad}

            Completed Tasks: {string.Join("\n", queueTasksForEval.Select(t => $"{t.Task} - Result: {t.Result}"))}
            ";

            var status = await BeginRunnerAsync<ProgressStatus>(prompt);

            CurrentScratchpad = status.UpdatedProgressSummary;

            if (status.Status == ProgressState.Stagnating || status.Status == ProgressState.Regression)
            {
                StallCount++;
            }
            else
            {
                StallCount = 0;
            }

            if(status.Status == ProgressState.Completed)
            {
                // Reset the scratchpad and task queue if the task is completed
                CurrentScratchpad = string.Empty;
                queueTasksForEval.Clear();
            }


            return new ProgressReport(status, StallCount);
        }

    }
}
