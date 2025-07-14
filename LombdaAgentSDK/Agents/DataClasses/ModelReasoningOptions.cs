namespace LombdaAgentSDK.Agents.DataClasses
{
    /// <summary>
    /// Reasoning effort level
    /// </summary>
    public enum ModelReasoningEffortLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Reasoning Summarization detail amount
    /// </summary>
    public enum ModelReasoningSummarizationDetail
    {
        None, //no summarization
        Basic, //concise summaries
        Detailed //detailed summaries with more context
    }

    /// <summary>
    /// Reasoning options to configure reasoning models
    /// </summary>
    public class ModelReasoningOptions
    {
        /// <summary>
        /// Reasoning effort level
        /// </summary>
        public ModelReasoningEffortLevel EffortLevel { get; set; }

        /// <summary>
        /// Reasoning Summarization detail amount
        /// </summary>
        public ModelReasoningSummarizationDetail SummarizationLevel { get; set; }

        public ModelReasoningOptions(ModelReasoningEffortLevel effortLevel = ModelReasoningEffortLevel.Medium)
        {
            EffortLevel = effortLevel;
        }
    }
}
