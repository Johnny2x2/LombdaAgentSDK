using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.Memory;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.States
{


    public class TaskDetailEnrichmentState : AgentState<QueueTask, EnrichmentResult>
    {
        public TaskDetailEnrichmentState(StateMachine stateMachine) : base(stateMachine)
        {
        }

        public override Agent InitilizeStateAgent()
        {
            string instructions = $"""

                You are an Enrichment Agent in an AI automation framework. Your primary role is to analyze the execution result from a recently completed task and enrich this information to make it more informative, actionable, and contextually useful for future retrieval and decision-making processes.

            Instructions:
                1. Review the provided task description and execution results in detail.

                2. Summarize the results clearly and concisely, highlighting the essential points and outcomes.

                3. Identify and clearly state any key insights, patterns, anomalies, or notable findings from the execution results.

                4. Categorize the task and results into appropriate tags or categories for efficient retrieval in the future.

                5. Recommend any potential follow-up actions or tasks that could further enhance or clarify the results or understanding, if relevant.

                6. Assess the importance or relevance of the result to future tasks, clearly indicating if this enriched data should be stored in long-term memory.

                7. If any additional context or clarification is needed from external sources, explicitly specify these requirements.
            """;

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(
                client,
                "Function Executor",
                instructions,
                _output_schema: typeof(EnrichmentResult));
        }

        public override async Task<EnrichmentResult> Invoke(QueueTask taskResult)
        {
            var QueriedResults = BabyAGIMemory.QueryLongTermMemory($"TASK:{taskResult.Task}\n TASK RESULT:{taskResult.Result}");

            string prompt = 
            $@"TASK:{taskResult.Task} 

            TASK RESULT:{taskResult.Result}

            Queried Results: 
            {QueriedResults} 

            ";

            EnrichmentResult taskEnrichment= await BeginRunnerAsync<EnrichmentResult>(prompt);

            return taskEnrichment;
        }
    }
}