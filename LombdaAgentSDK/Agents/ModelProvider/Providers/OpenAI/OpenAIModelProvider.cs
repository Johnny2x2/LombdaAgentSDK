using OpenAI;
using OpenAI.Responses;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System.ClientModel;
using System.Drawing;
using static LombdaAgentSDK.Runner;
using LlmTornado.Code;
using LlmTornado.Chat.Models;

namespace LombdaAgentSDK
{
#pragma warning disable OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    [Obsolete("Obsoleting OpenAIModelClient so we can use 1 middle man provider for all Providers... Please Use LLMTornadoModelProvider Class")]
    public partial class OpenAIModelClient : ModelClient
    {
        //Used to save the results of the computer response
        public Dictionary<string, ComputerOutputResult> ComputerResults = new Dictionary<string, ComputerOutputResult>();

        //Client Specific Properties

        public LLMTornadoModelProvider Client { get; set; }

        public OpenAIClientOptions Options { get; set; }
        public ApiKeyCredential ApiKeyCredential { get; set; }

        //Open AI Tool specific properties
        public bool EnableWebSearch { get; set; } = false;
        public bool EnableComputerCalls { get; set; } = false;
        public OpenAIFileSearchOptions? FileSearchOptions { get; set; }

        public OpenAIModelClient(
            string model, ApiKeyCredential _apiKeyCredential = null, OpenAIClientOptions _clientOptions = null, 
            bool enableWebSearch = false, bool enableComputerCalls = false,
            OpenAIFileSearchOptions? vectorDbOptions = null
            )
        {

            Model = model;

            EnableWebSearch = enableWebSearch;
            EnableComputerCalls = enableComputerCalls;
            FileSearchOptions = vectorDbOptions ?? FileSearchOptions;

            Client = new LLMTornadoModelProvider(
                new ChatModel(model),
                [new ProviderAuthentication(LLmProviders.OpenAi, SetAndValidateApiKey(_apiKeyCredential))],
                useResponseAPI: true,
                allowComputerUse: enableComputerCalls,
                enableWebSearch: enableWebSearch
                );

            if (string.IsNullOrEmpty(Model))
            {
                throw new ArgumentException("Model name cannot be null or empty.", nameof(model));
            }


            Options = _clientOptions == null ? new OpenAIClientOptions() : _clientOptions;

            if(_clientOptions is not null)
            {
                Console.WriteLine("Using Custom OpenAI Client Options no longer supported (using LLMTornado backend)");
            }
        }

        private string SetAndValidateApiKey(ApiKeyCredential? _apiKeyCredential = null)
        {
            ApiKeyCredential = _apiKeyCredential == null ? new ApiKeyCredential(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) : _apiKeyCredential;

            string apiKey = string.Empty;

            ApiKeyCredential?.Deconstruct(out apiKey);

            if (ApiKeyCredential == null || string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API Key cannot be null or empty. Try Setting Enviornment Variable OPENAI_API_KEY", nameof(_apiKeyCredential));
            }

            return apiKey;
        }

        public override async Task<ModelResponse> CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            return await Client.CreateStreamingResponseAsync(messages, options, streamingCallback);
        }

        public override async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            
            return await Client.CreateResponseAsync(messages, options);
        }
    }

    public class OpenAIFileSearchOptions
    {
        public FileSearchToolRankingOptions RankingOptions { get; set; } = new FileSearchToolRankingOptions();
        public List<string> VectorIDs { get; set; } = new List<string>();

        public int MaxResults { get; set; } = 10;

        public BinaryData Filters { get; set; }

        public OpenAIFileSearchOptions(List<string>? vectorDbID = null, FileSearchToolRankingOptions? options = null, int maxResults = 10, BinaryData filters = null)
        {
            VectorIDs = vectorDbID ?? VectorIDs;
            RankingOptions = options ?? new FileSearchToolRankingOptions();
            MaxResults = maxResults;
            Filters = filters;
        }
    }

    public class ComputerOutputResult
    {
        public string ImageUrl { get; set; } = "";

        public string CallId { get; set; }

        public ComputerOutputResult() { }

        public ComputerOutputResult(string callId, string dataUrl)
        {
            CallId = callId;
            ImageUrl = dataUrl;
        }
    }

#pragma warning restore OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
