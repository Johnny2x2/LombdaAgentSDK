using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.DataModels
{
    public struct TaskItem
    {
        public int TaskId { get; set; }
        public string Description { get; set; }
        public string ExpectedOutcome { get; set; }
        public string[] Dependencies { get; set; }
        public string Complexity { get; set; } // "Low", "Medium", "High"
        public string SuccessCriteria { get; set; }

        public override string ToString()
        {
            return $"TaskId: {TaskId}, Description: {Description}, ExpectedOutcome: {ExpectedOutcome}, " +
                   $"Dependencies: [{string.Join(", ", Dependencies ?? Array.Empty<string>())}], " +
                   $"Complexity: {Complexity}, SuccessCriteria: {SuccessCriteria}";
        }
    }

    public struct bTaskItem
    {
        public int TaskId { get; set; }
        public string Description { get; set; }
        public string SuccessCriteria { get; set; }

        public override string ToString()
        {
            return $"TaskId: {TaskId}, Description: {Description}, ExpectedOutcome: {SuccessCriteria}";
        }

    }

    public struct TaskBreakdownResult
    {
        public bTaskItem[] Tasks { get; set; }
        public string OverallStrategy { get; set; }
    }
}
