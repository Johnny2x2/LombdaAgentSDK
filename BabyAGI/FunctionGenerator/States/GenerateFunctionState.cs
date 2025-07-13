using Examples.Demos.CodingAgent;
using Examples.Demos.FunctionGenerator;
using Examples.Demos.FunctionGenerator.States;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public class GenerateFunctionState : BaseState<FunctionBreakDownResults, FunctionBreakDownResults>
    {
        public FunctionGeneratorAgent StateController { get; set; }
        public GenerateFunctionState(FunctionGeneratorAgent stateController) { StateController = stateController; }
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
            CSHARP_CodingAgent codeAgent = new CSHARP_CodingAgent(StateController.FunctionsPath);
            await codeAgent.RunCodingAgent(new FunctionBreakDownInput("", function));
        }
    }
}
