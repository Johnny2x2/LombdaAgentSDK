using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.States;
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

namespace BabyAGI.Agents.ProjectCodingAgent.states
{
    public class ProjectCodeTaskCreatorState : BaseState<ProgramDesignResult, TaskBreakdownResult>
    {
        public ProgramDesignResult? CurrentDesignRequirements { get; set; }
        public Dictionary<string, ProgramApprovalResult> ProgramApprovalResults { get; set; } = new();
        public List<List<TaskItem>> GeneratedTask { get; set; } = new();

        public override async Task<TaskBreakdownResult> Invoke(ProgramDesignResult input)
        {
            if(CurrentDesignRequirements == null)
            {
                // If we already have design requirements, use them
                CurrentDesignRequirements = input;
            }

            string historicApprovalAttempts = "";

            for(int i = 0; i < ProgramApprovalResults.Count; i++)
            {
                var kvp = ProgramApprovalResults.ElementAt(i);
                //Task ran before the appproval
                historicApprovalAttempts += string.Join("\n", GeneratedTask[i]);
                historicApprovalAttempts += $"Failed: {kvp.Value.Approval.Reason}\n\n";
                historicApprovalAttempts += "------------------------------------------";
            }


            if (input.ApprovalResult != null)
            {
                // If we have an approval result, add it to the list
                ProgramApprovalResults[input.ApprovalResult.Id] = input.ApprovalResult!;
            }


            string prompt = $@"
            Project Goal: {CurrentDesignRequirements?.Request}

            Program Design Description: {CurrentDesignRequirements?.Design.Description}

            Input Args: {CurrentDesignRequirements?.Design.ExpectedInputArgs}

            Expected Results: {CurrentDesignRequirements?.Design.ExpectedResult}

            Success Criteria: {CurrentDesignRequirements?.Design.SuccessCriteria}
            
            Current Approval Status:
            { string.Join("\n", ProgramApprovalResults.Select(kvp => $"Failed: {kvp.Value.Approval.Reason}")) }

            Previous Approval Attempts:
            {historicApprovalAttempts}

            Context: This is part of a task management system where tasks will be executed sequentially by automated agents.
            Each task should be:
            - Specific and actionable
            - Self-contained (can be executed independently)
            - Measurable (clear success criteria)
            - Appropriately scoped (not too broad or too narrow)

            Current Task Queue Status: {GetTaskQueueStatus()}
            ";

            string instructions = $"""
            You are an expert task breakdown specialist for an AI agent system. Your role is to decompose user goals into a well-structured sequence of executable tasks.

            REQUIREMENTS:
            1. Break down the user goal into 3-7 specific, actionable tasks
            2. Each task must be executable by an AI agent without human intervention
            3. Tasks should follow logical dependencies (earlier tasks enable later ones)
            4. Include clear success criteria for each task
            5. Consider error handling and validation steps where appropriate

            OUTPUT FORMAT:
            Provide an ordered list where each task includes:
            - Task ID (sequential number)
            - Clear, action-oriented description
            - Expected outcome/deliverable
            - Dependencies (if any)
            - Estimated complexity (Low/Medium/High)

            EXAMPLES OF GOOD TASKS:
            - "Research Python web frameworks and compile a comparison table"
            - "Create project directory structure with standard folders"
            - "Generate unit test template for the authentication module"

            EXAMPLES OF POOR TASKS:
            - "Make it work" (too vague)
            - "Do everything related to the database" (too broad)
            - "Fix bugs" (not specific enough)

            Focus on creating tasks that move systematically toward the user's goal while being concrete enough for an AI executor to understand and complete.
            """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "Task Generator",
                instructions,
                _output_schema: typeof(TaskBreakdownResult));

            RunResult result = await Runner.RunAsync(agent, prompt);

            TaskBreakdownResult taskResults = result.ParseJson<TaskBreakdownResult>();

            GeneratedTask.Add(new(taskResults.Tasks));

            return taskResults;
        }

        private string GetTaskQueueStatus()
        {
            if (CurrentStateMachine?.RuntimeProperties?.ContainsKey("TaskQueue") == true)
            {
                var queue = (Queue<QueueTask>)CurrentStateMachine.RuntimeProperties["TaskQueue"];
                return $"Current queue has {queue.Count} pending tasks";
            }
            return "Task queue is empty - this will be the first task";
        }
    }
}
