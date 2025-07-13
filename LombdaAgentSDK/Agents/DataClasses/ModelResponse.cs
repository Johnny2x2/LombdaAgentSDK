namespace LombdaAgentSDK.Agents.DataClasses
{
    public class ModelResponse
    {
        public string? Id { get; set; }
        public ModelOutputFormat? OutputFormat { get; set; }

        public string? Model { get; set; }

        public List<ModelItem> Messages { get; set; } = new List<ModelItem>();

        public List<ModelItem>? OutputItems { get; set; }

        public ModelResponse() { }

        [Obsolete]
        public ModelResponse(string model, List<ModelItem> outputItems = null, ModelOutputFormat outputFormat = null, List<ModelItem> messages = null)
        {
            Model = model;
            OutputItems = outputItems;
            OutputFormat = outputFormat;
            Messages = messages ?? new List<ModelItem>();
        }

        public ModelResponse(List<ModelItem> outputItems = null, ModelOutputFormat outputFormat = null, List<ModelItem> messages = null, string? id = null)
        {
            OutputItems = outputItems;
            OutputFormat = outputFormat;
            Messages = messages ?? new List<ModelItem>();
            Id = id;
        }
    }
}
