using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.ProjectCodingAgent.DataModels
{
    public struct ProgramDesign
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string ExpectedInputArgs { get; set; }
        public string ExpectedResult { get; set; }
        public string SuccessCriteria { get; set; }
    }

    public struct ProgramApproval
    {
        public bool Approved { get; set; }
        public string Reason { get; set; }
    }
}
