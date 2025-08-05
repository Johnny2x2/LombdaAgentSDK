using BabyAGI.FunctionGenerator.DataModels;
using Examples.Demos.CodingAgent;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public class GenerateFunctionState : AgentState<FunctionBreakDownResults, FunctionBreakDownResults>
    {
        string functionsPath = "";
        public GenerateFunctionState(StateMachine stateMachine) : base(stateMachine) {  }

        public override Agent InitilizeStateAgent()
        {
            //Dummy agent for generating functions based on the breakdown results. need to conver state machine and make a state to represent this.
            return new Agent(new OpenAIModelClient("gpt-4o"), "GenerateFunctionState", "Generates functions based on the breakdown results.");
        }

        public override async Task<FunctionBreakDownResults> Invoke(FunctionBreakDownResults breakDownResults)
        {
            List<Task> tasks = new();

            foreach (var function in breakDownResults.FunctionsToGenerate)
            {
                tasks.Add(Task.Run(async () => await RunAgent(function)));
            }

            await Task.WhenAll(tasks);

            return breakDownResults;
        }

        async Task RunAgent(FunctionBreakDown function)
        {
            if (string.IsNullOrEmpty(functionsPath))
            {
                functionsPath = CurrentStateMachine.RuntimeProperties.TryGetValue("FunctionsPath", out object? path) ? path?.ToString() : null;
                if (string.IsNullOrEmpty(functionsPath))
                    throw new InvalidOperationException("FunctionsPath is not set in the runtime properties.");
            }

            if(CurrentStateMachine is IAgentStateMachine agentStateMachine)
            {
                var controller =agentStateMachine.ControlAgent;
                ToolCodingAgent codeAgent = new ToolCodingAgent(controller);
                await codeAgent.Run(new FunctionBreakDownInput("", function));
            }
        }
    }
}
