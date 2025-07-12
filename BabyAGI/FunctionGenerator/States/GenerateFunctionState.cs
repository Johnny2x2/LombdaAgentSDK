using Examples.Demos.FunctionGenerator.States;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public class GenerateFunctionState : BaseState<FunctionBreakDownResults, FunctionFoundResultOutput>
    {
        public override Task<FunctionFoundResultOutput> Invoke(FunctionBreakDownResults input)
        {
            throw new NotImplementedException();
        }
    }
}
