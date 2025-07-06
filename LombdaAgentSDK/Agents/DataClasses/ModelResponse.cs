namespace LombdaAgentSDK.Agents.DataClasses
{
    public class ModelResponse
    {
        public ModelOutputFormat? OutputFormat { get; set; }

        public string? Model { get; set; }

        public List<ModelItem> Messages { get; set; } = new List<ModelItem>();

        public List<ModelItem>? OutputItems { get; set; }

        public ModelResponse() { }

        public ModelResponse(string model, List<ModelItem> outputItems = null, ModelOutputFormat outputFormat = null, List<ModelItem> messages = null)
        {
            Model = model;
            OutputItems = outputItems;
            OutputFormat = outputFormat;
            Messages = messages ?? new List<ModelItem>();
        }
    }
}
