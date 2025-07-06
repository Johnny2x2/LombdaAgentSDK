namespace LombdaAgentSDK.Agents.DataClasses
{
    public enum ModelReasoningEffortLevel
    {
        Low,
        Medium,
        High
    }
    
    public enum ModelReasoningSummarizationDetail
    {
        None, //no summarization
        Basic, //concise summaries
        Detailed //detailed summaries with more context
    }

    public class ModelReasoningOptions
    {
        public ModelReasoningEffortLevel EffortLevel { get; set; }
        public ModelReasoningSummarizationDetail SummarizationLevel { get; set; }

        public ModelReasoningOptions(ModelReasoningEffortLevel effortLevel = ModelReasoningEffortLevel.Medium)
        {
            EffortLevel = effortLevel;
        }
    }
}
