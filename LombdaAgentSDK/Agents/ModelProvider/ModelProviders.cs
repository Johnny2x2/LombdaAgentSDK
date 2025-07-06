using LombdaAgentSDK.Agents.DataClasses;
using static LombdaAgentSDK.Runner;

namespace LombdaAgentSDK
{
    public abstract class ModelClient
    {
        public string Model { get; set; }
        public string? API_KEY { get; set; }
        public ModelClient() { }

        public ModelClient(string model, string? apiKey = "")
        {
            Model = model;
            API_KEY = apiKey;
        }

        public async Task<ModelResponse> _CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            return await CreateStreamingResponseAsync(messages, options, streamingCallback);
        }

        public virtual async Task<ModelResponse> CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            return await CreateStreamingResponseAsync(messages, options, streamingCallback);
        }

        public async Task<ModelResponse> _CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options) 
        {
            return await CreateResponseAsync(messages, options);
        }
        public virtual async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            return new ModelResponse();
        }

        public virtual ModelResponse CreateResponse(List<ModelItem> messages, ModelResponseOptions options)
        {
            return new ModelResponse();
        }
        public void _SetModel(string model)
        {
            SetModel(model);
        }

        public virtual void SetModel(string model)
        {
            Model = model;
        }
    }
}
