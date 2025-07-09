using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Examples.Demos.CodeUtility;

namespace Examples.Demos.CodingAgent.states
{
    //Need to generate result struct
    class CSharpBuildState : BaseState<ProgramResultOutput, CodeBuildInfoOutput>
    {
        public override async Task<CodeBuildInfoOutput> Invoke(ProgramResultOutput programResult)
        {
            //Need a file path to a solution you don't care about or has git control
            

            if (!Directory.Exists(FileIOUtility.SafeWorkingDirectory))
            {
                throw new Exception("Need to set a directory to a c# project");
            }

            //Write over files in project
            foreach (CodeItem script in programResult.Result.items)
            {
                FileIOUtility.WriteFile(script.filePath, script.code);
            }

            //build the project code
            //In theory here i could setup a lot of different code to build
            CodeBuildInfo codeInfo = CodeUtility.BuildAndRunProject(CSHARP_CodingAgent.ProjectBuildPath, CSHARP_CodingAgent.ProjectName);

            //Report the results of the build
            return new CodeBuildInfoOutput(codeInfo, programResult);
        }
    }
}
