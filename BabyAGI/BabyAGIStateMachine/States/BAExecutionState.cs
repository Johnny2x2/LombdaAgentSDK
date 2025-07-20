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
        public UserInputRequestDelegate userInputRequest { get; set; }

        public BAExecutionState(StateMachine stateMachine) : base(stateMachine)
        {
            if (stateMachine is IAgentStateMachine agentStateMachine)
            {
                agentStateMachine.ControlAgent.UserInputRequested += userInputRequest;
            }
        }

        public override Agent InitilizeStateAgent()
        {
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], enableWebSearch: true, useResponseAPI: true);

            string instructions = $"""You are a person assistant AGI with the ability to generate tools to answer any user question if you cannot do it directly task your tool to create it.""";

            return new Agent(client, "BabyAGI", instructions, _tools: [AttemptToCompleteTask, ControlComputer, DoResearch, QueryLongTermMemory, QueryShortTermMemory, RequestUserInput]);
        }

        public override async Task<QueueTask> Invoke(QueueTask input)
        {
            string prompt =$@"<TASK>: {input.Task}</TASK> ";

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
                ComputerControllerAgent computerTool = new ComputerControllerAgent(cancellationTokenSource:CancelTokenSource, verboseCallback:RunnerVerboseCallbacks);
                return await computerTool.RunComputerAgent(task);
            }
            catch
            {
                return "Error: Could not control computer. Ensure you have the correct permissions and API access.";
            }
        }


        [Tool(Description = "Use this to query long term memory for context [CAUTION MAY OR MAY NOT BE RELEVANT]", In_parameters_description = ["Query you wish to search"])]
        public async Task<string> QueryLongTermMemory(string search)
        {
            return string.Join("\n\n", await BabyAGIMemory.QueryLongTermMemory(search));
        }

        [Tool(Description = "Use this to query short term memory for context [CAUTION MAY OR MAY NOT BE RELEVANT]", In_parameters_description = ["Query you wish to search"])]
        public async Task<string> QueryShortTermMemory(string search)
        {
            return string.Join("\n\n", await BabyAGIMemory.QueryShortTermMemory(search));
        }

        [Tool(Description = "Use this to ask user for input", In_parameters_description = ["question you want to ask"])]
        public string RequestUserInput(string prompt)
        {
            // Get the Result property from the Task
            return (string?)userInputRequest?.DynamicInvoke(prompt)!;
        }
    }
}
