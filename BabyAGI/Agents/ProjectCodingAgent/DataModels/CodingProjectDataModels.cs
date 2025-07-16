using BabyAGI.BabyAGIStateMachine.States;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.ProjectCodingAgent.DataModels
{
    public struct ProjectResultOutput
    {
        public ProgramResult Result { get; set; }
        public TaskItem CurrentTask { get; set; }
        public ProjectResultOutput(ProgramResult result, TaskItem request)
        {
            Result = result;
            CurrentTask = request;
        }
    }

    public struct CodeProjectBuildInfoOutput
    {
        public CodeBuildInfo BuildInfo { get; set; }
        public ProjectResultOutput ProgramResult { get; set; }

        public CodeProjectBuildInfoOutput() { }
        public CodeProjectBuildInfoOutput(CodeBuildInfo info, ProjectResultOutput codeResult)
        {
            BuildInfo = info;
            ProgramResult = codeResult;
        }
    }
}
