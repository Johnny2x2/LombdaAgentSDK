using Examples.Demos.CodingAgent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.ProjectCodingAgent.DataModels
{

    [Description("Represents the requirements for the program")]
    public struct ProgramDesign
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string ExpectedInputArgs { get; set; }
        public string ExpectedResult { get; set; }
        public string SuccessCriteria { get; set; }
    }

    [Description("Output a list of command line args for the exe execution to test the project (The system will handle adding the exe to the command line so don't include that)")]
    public struct ProjectInputs
    {
        public string[] commandline_argument_examples { get; set; }
    }

    public struct ProgramApproval
    {
        public bool Approved { get; set; }
        public string Reason { get; set; }
    }
}
