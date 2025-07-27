using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentSDK.Agents.DataClasses
{
    public class ModelCodeInterpreterOptions
    {
        public string ContainerId { get; set; } = "";
        public List<string> FileIds { get; set; } = new List<string>();
        public ModelCodeInterpreterOptions() { }
    }
}
