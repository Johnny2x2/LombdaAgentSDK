using Examples.Demos.FunctionGenerator.States;
using LombdaAgentSDK.StateMachine;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.FunctionGenerator
{
    public class FunctionGeneratorAgent
    {
        [Test]
        public async Task RunAgent()
        {
            BreakDownTaskState breakDownTaskState = new BreakDownTaskState();
            breakDownTaskState.AddTransition(_ => true, new ExitState());
            StateMachine<FunctionFoundResultOutput, FunctionBreakDownResults> stateMachine = new();

            stateMachine.SetEntryState(breakDownTaskState);
            stateMachine.SetOutputState(breakDownTaskState);

            var result = await stateMachine.Run(new FunctionFoundResultOutput("what is the weather?", new FunctionFoundResult("", false)));
        }
    }
}
