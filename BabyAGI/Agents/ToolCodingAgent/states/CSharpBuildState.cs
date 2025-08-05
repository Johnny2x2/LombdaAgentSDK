using BabyAGI;
using BabyAGI.Utility;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Examples.Demos.CodingAgent.states
{
    //Need to generate result struct
    class CSharpBuildState : BaseState<ProgramResultOutput, CodeBuildInfoOutput>
    {
        string ProjectName = string.Empty;
        public CSharpBuildState(StateMachine stateMachine)
        {

        }

        public override async Task<CodeBuildInfoOutput> Invoke(ProgramResultOutput programResult)
        {
            if (string.IsNullOrEmpty(ProjectName))
            {
                ProjectName = CurrentStateMachine.RuntimeProperties.TryGetValue("ProjectName", out object pName) ? pName.ToString() : string.Empty;
                if (string.IsNullOrEmpty(ProjectName))
                {
                    throw new InvalidOperationException("ProjectName cannot be empty");
                }
            }
            //Write over files in project
            foreach (CodeItem script in programResult.Result.items)
            {
                FunctionGeneratorUtility.WriteToProject(BabyAGIConfig.FunctionsPath, ProjectName, script.filePath, script.code);
            }

            //build the project code
            //In theory here i could setup a lot of different code to build
            CodeBuildInfo codeInfo = FunctionGeneratorUtility.BuildAndRunProject(BabyAGIConfig.FunctionsPath, ProjectName, "net8.0", programResult.Result.Sample_EXE_Args, true);

            //Report the results of the build
            return new CodeBuildInfoOutput(codeInfo, programResult);
        }
    }
}
