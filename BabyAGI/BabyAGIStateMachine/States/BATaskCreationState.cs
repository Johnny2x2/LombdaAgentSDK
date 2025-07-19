using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.Utility;
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

namespace BabyAGI.BabyAGIStateMachine.States
{
   
    public class BATaskCreationState : AgentState<string, TaskBreakdownResult>
    {
        public override async Task<TaskBreakdownResult> Invoke(string input)
        {
            var currentTask = new QueueTask();

            string prompt = $@"
            User Goal: {input}

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
                "Function Executor",
                instructions,
                _output_schema: typeof(TaskBreakdownResult));

            RunResult result = await Runner.RunAsync(agent, prompt);

            TaskBreakdownResult taskResults = result.ParseJson<TaskBreakdownResult>();

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
