namespace LombdaAgentSDK.Agents.DataClasses
{
    public class RunResult
    {
        

        private List<ModelItem> messages = new List<ModelItem>();

        private bool guardrailTriggered = false;

        public ModelResponse Response { get; set; } = new ModelResponse();
        public List<ModelItem> Messages { get => messages; set => messages = value; }

        public bool GuardrailTriggered { get => guardrailTriggered; set => guardrailTriggered = value; }

        public string? Text => TryGetText();

        public string? TryGetText()
        {
            try
            {
                return ((ModelMessageItem?)Response.OutputItems?.LastOrDefault())?.Text ?? string.Empty;
            }
            catch
            {
                return null;
            }
        }
        public RunResult() { }
    }

}
