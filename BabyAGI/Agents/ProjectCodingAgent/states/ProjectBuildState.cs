using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;

using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Examples.Demos.ProjectCodingAgent.states
{
    //Need to generate result struct
    class ProjectBuildState : BaseState<ProjectResultOutput, CodeProjectBuildInfoOutput>
    {
        public CodingProjectsAgent StateAgent { get; set; }

        public ProjectBuildState(CodingProjectsAgent stateAgent) 
        {
            StateAgent = stateAgent;
        }

        public override async Task<CodeProjectBuildInfoOutput> Invoke(ProjectResultOutput programResult)
        {
            if(StateAgent.FunctionsPath == null || StateAgent.FunctionsPath == "")
            {
                throw new ArgumentException("FunctionsPath is not set in the CodingProjectsAgent.");
            }

            //Write over files in project
            foreach (CodeItem script in programResult.Result.items)
            {
                FunctionGeneratorUtility.WriteToProject(StateAgent.FunctionsPath, StateAgent.ProjectName, script.filePath, script.code);
            }

            //build the project code
            //In theory here i could setup a lot of different code to build
            CodeBuildInfo codeInfo = FunctionGeneratorUtility.BuildAndRunProject(StateAgent.FunctionsPath, StateAgent.ProjectName, "net8.0", programResult.Result.Sample_EXE_Args, true);

            //Report the results of the build
            return new CodeProjectBuildInfoOutput(codeInfo, programResult);
        }

    }
}
