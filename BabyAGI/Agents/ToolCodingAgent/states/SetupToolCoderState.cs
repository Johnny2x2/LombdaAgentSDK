using BabyAGI.FunctionGenerator.DataModels;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.ToolCodingAgent.states
{
    public class SetupToolCoderState : BaseState<FunctionBreakDownInput, string>
    {
        public override async Task<string> Invoke(FunctionBreakDownInput context)
        {
            var projectName = context.functionBreakDown.FunctionName;

            CurrentStateMachine.RuntimeProperties.AddOrUpdate("ProjectName", projectName, 
                (okey, ovalue) =>
                {
                    try
                    {
                        return projectName;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to set project Name {projectName}. Error: {ex.Message}");
                    }
                });

            FunctionGeneratorUtility.CreateNewProject(BabyAGIConfig.FunctionsPath, context.functionBreakDown.FunctionName, context.functionBreakDown.Description);

            return $"User Request {context.Context} \n\n Function context: {context.ToString()}";
        }
    }
}
