using LombdaAgentSDK.Agents.Tools;

namespace LombdaAgentSDK.Agents.DataClasses
{
    public class ModelResponseOptions
    {
        public string? PreviousResponseId {  get; set; }

        public string Model { get; set; }

        public string Instructions { get; set; }

        public List<BaseTool> Tools { get; set; } = new List<BaseTool>();

        public ModelOutputFormat OutputFormat { get; set; }

        public ModelReasoningOptions ReasoningOptions { get; set; }

        public ModelResponseOptions() { }
    }
}
