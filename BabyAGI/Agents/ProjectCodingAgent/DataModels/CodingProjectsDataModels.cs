using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.States;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using Examples.Demos.FunctionGenerator.States;
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


    public struct ProgramDesignResult
    {
        public ProgramDesign Design { get; set; }
        public string Request { get; set; }
        public ProgramApprovalResult ApprovalResult { get; set; }
    }

    public class ProgramApprovalResult
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public ProgramApproval Approval { get; set; }
        public CodeBuildInfoOutput CodeBuildInfo { get; set; }
    }


}
