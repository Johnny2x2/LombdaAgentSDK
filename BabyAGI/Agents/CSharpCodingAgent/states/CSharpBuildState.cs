using BabyAGI.Utility;
using LombdaAgentSDK.StateMachine;
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
        public CSHARP_CodingAgent StateAgent { get; set; }
        public CSharpBuildState(CSHARP_CodingAgent stateAgent) 
        {
            StateAgent = stateAgent;
        }
        public override async Task<CodeBuildInfoOutput> Invoke(ProgramResultOutput programResult)
        {
            //Write over files in project
            foreach (CodeItem script in programResult.Result.items)
            {
                FunctionGeneratorUtility.WriteToProject(StateAgent.FunctionsPath, StateAgent.ProjectName, script.filePath, script.code);
            }

            //build the project code
            //In theory here i could setup a lot of different code to build
            CodeBuildInfo codeInfo = FunctionGeneratorUtility.BuildAndRunProject(StateAgent.FunctionsPath, StateAgent.ProjectName, "net8.0", programResult.Result.Sample_EXE_Args, true);

            //Report the results of the build
            return new CodeBuildInfoOutput(codeInfo, programResult);
        }
    }
}
