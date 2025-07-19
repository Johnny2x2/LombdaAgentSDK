using BabyAGI.Agents;
using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Agents.ResearchAgent;
using BabyAGI.Agents.ResearchAgent.DataModels;
using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.Memory;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.States
{

    public class BAExecutionState : AgentState<QueueTask, QueueTask>
    {
        public BAExecutionState(StateMachine stateMachine) : base(stateMachine)
        {

        }

        public override Agent InitilizeStateAgent()
        {
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], enableWebSearch:true, useResponseAPI: true);

            string instructions = $"""You are a person assistant AGI with the ability to generate tools to answer any user question if you cannot do it directly task your tool to create it.""";

            return new Agent(client, "BabyAGI", instructions, _tools: [AttemptToCompleteTask, ControlComputer, DoResearch]);
        }

        public override async Task<QueueTask> Invoke(QueueTask input)
        {
            string prompt = 
                $@"
<TASK>: {input.Task}</TASK> 

<SHORT_TERM_MEMORY_CONTEXT>: {string.Join("\n\n",BabyAGIMemory.QueryShortTermMemory(input.Task))}</SHORT_TERM_MEMORY_CONTEXT>

<LONG_TERM_MEMORY_CONTEXT>{string.Join("\n\n", BabyAGIMemory.QueryLongTermMemory(input.Task))}</LONG_TERM_MEMORY_CONTEXT>
";

            input.Result = await BeginRunnerAsync(prompt);

            return input;
        }

        /// <summary>
        /// Attempts to complete the specified task using the agent system.
        /// </summary>
        /// <remarks>This method utilizes a function generator system to process and attempt to complete
        /// the given task.  It is recommended to use this method before informing a user of an inability to perform a
        /// task.</remarks>
        /// <param name="root">The root agent initiating the task completion process.</param>
        /// <param name="task">The description of the task to be completed.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with a result containing the
        /// assistant's message upon task completion.</returns>
        [Tool(Description = "Use this before telling a user you are unable to do something", In_parameters_description = ["The task you wish to accomplish."])]
        public async Task<string> AttemptToCompleteTask(string task)
        {
            FunctionGeneratorAgent generatorSystem = new(((IAgentStateMachine)CurrentStateMachine!).ControlAgent, BabyAGIConfig.FunctionsPath);
            var finalResult = await generatorSystem.Run(task);
            return finalResult[0].AssistantMessage;
        }

        /// <summary>
        /// Performs deep web research on the specified topic and returns a comprehensive report.
        /// </summary>
        /// <remarks>This method utilizes a research agent to conduct an in-depth analysis of the given
        /// topic by exploring various web sources. The returned report is generated based on the findings from the
        /// research process.</remarks>
        /// <param name="topic">The topic to research. Cannot be null or empty.</param>
        /// <returns>A string containing the final report of the research. The report provides detailed findings on the specified
        /// topic.</returns>
        [Tool(Description = "Use this tool for doing deep web research", In_parameters_description = ["The topic you wish to research."])]
        public async Task<string> DoResearch(string topic)
        {
            ResearchAgent researchTool = new ResearchAgent(((IAgentStateMachine)CurrentStateMachine!).ControlAgent);
            //Run the state machine
            List<ReportData> reports = await researchTool.Run(topic);
            //Report on the last state with Results
            return reports[0].FinalReport;
        }

        // Seems to require a little persuasion to get to use tool instead of openAI being like nah I can't do that.
        //requires Teir 3 account & access to use computer-use-preview currently
        [Tool(Description = "Use this agent to accomplish task that require computer input or output like mouse movement, clicking, screen shots", In_parameters_description = ["The task you wish to accomplish."])]
        public async Task<string> ControlComputer(string task)
        {
            try
            {
                ComputerControllerAgent computerTool = new ComputerControllerAgent();
                return await computerTool.RunComputerAgent(task);
            }
            catch
            {
                return "Error: Could not control computer. Ensure you have the correct permissions and API access.";
            }
        }

        /// <summary>
        /// Performs a basic web search for the specified topic.
        /// </summary>
        /// <remarks>This method utilizes an external web search tool to retrieve information about the
        /// specified topic. Ensure that the necessary API key is set in the environment variables for successful
        /// execution.</remarks>
        /// <param name="search">The topic to research. This parameter cannot be null or empty.</param>
        /// <returns>A <see cref="string"/> containing the search results. Returns "Error: Could not search web." if the search
        /// fails.</returns>
        [Tool(Description = "Use this tool for doing basic web search", In_parameters_description = ["The topic you wish to research."])]
        public async Task<string> BasicWebSearch(string search)
        {
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], enableWebSearch: true);
            Agent agent = new Agent(
                client,
                "Web searcher",
                "Using WebSearch and search for results of the given task.");

            RunResult result = await Runner.RunAsync(agent, search, cancellationToken: StateAgent.Client.CancelTokenSource);

            return result.Text ?? "Error: Could not search web.";
        }

    }
}
