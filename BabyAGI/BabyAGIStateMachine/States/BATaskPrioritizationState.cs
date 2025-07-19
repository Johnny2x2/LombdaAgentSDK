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
    public class BATaskPrioritizationState : AgentState<string, QueueTask>
    {
        public BATaskPrioritizationState(StateMachine stateMachine) : base(stateMachine)
        {
        }

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""
            You are a Task Prioritization Agent within an AI-driven automation system. Your responsibility is to review a set of candidate tasks, analyze their context and metadata, and output the most effective order for execution based on urgency, dependencies, and impact.
            
            Instructions:

            Analyze the list of tasks, including their descriptions, objectives, metadata (such as deadlines, dependencies, categories, estimated effort, or impact).

            Identify any dependencies or prerequisites between tasks (e.g., Task B cannot start until Task A is complete).

            Assess urgency and importance for each task, considering explicit priorities, deadlines, and potential business or system impact.

            Rank the tasks in the optimal execution order, with highest-priority/most-urgent tasks first.

            Justify your prioritization for each task with a clear explanation (referencing urgency, dependencies, or other relevant factors).

            Do not remove or alter tasks—simply re-order and annotate them.
            """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Prioritizer",
                instructions,
                _output_schema: typeof(TaskBreakdownResult));
        } 


        public override async Task<QueueTask> Invoke(string input)
        {
            return new QueueTask(input);
        }
    }
}
