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
    public struct TaskGeneratorResult
    {
        public string Reasoning { get; set; }
        public bTaskItem[] NewTasks { get; set; }
    }

    internal class BATaskGeneratorState : AgentState<string, List<QueueTask>>
    {
        public BATaskGeneratorState(StateMachine stateMachine) : base(stateMachine)
        {
        }

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""

                You are a Task Generator Agent in an AI-driven automation framework. 
                Your primary function is to analyze execution results from previously performed tasks and clearly identify if additional steps or new tasks are required.

            Instructions:

                1. Carefully analyze the provided execution result, including any outputs, errors, logs, or observations.

                2. Determine whether the provided results fulfill the original intent or requirements of the previously executed task.

                3. If the execution result is incomplete, incorrect, or could benefit from further clarification or refinement, explicitly describe the missing aspects or what needs improvement.

                4. Clearly specify any new tasks that should be performed, including concise instructions, objectives, and relevant details.

                5. If the results fully satisfy the original task objective and no further action is needed, explicitly state that the task is complete and no additional steps are necessary.

                6.Maintain logical consistency and avoid repeating previously completed tasks or generating irrelevant tasks.
            """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Function Executor",
                instructions,
                _output_schema: typeof(TaskGeneratorResult));
        }

        public override async Task<List<QueueTask>> Invoke(string taskResult)
        {
            string prompt = $@"{taskResult}";

            TaskGeneratorResult taskStatus = await BeginRunnerAsync<TaskGeneratorResult>(prompt);

            List<QueueTask> taskQueue = new List<QueueTask>();

            foreach (var newTask in taskStatus.NewTasks)
            {
                if (!string.IsNullOrEmpty(newTask.ToString()))
                {
                    // Add the new task to the queue
                    taskQueue.Add(new QueueTask
                    {
                        Task = newTask.ToString()
                    });

                }
            }

            return taskQueue;
        }
    }
}
