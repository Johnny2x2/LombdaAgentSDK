using OpenAI;
using OpenAI.Responses;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System.ClientModel;
using System.Drawing;
using static LombdaAgentSDK.Runner;

namespace LombdaAgentSDK
{
#pragma warning disable OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public partial class OpenAIModelClient : ModelClient
    {
        //Used to save the results of the computer response
        public Dictionary<string, ComputerOutputResult> ComputerResults = new Dictionary<string, ComputerOutputResult>();

        //Client Specific Properties

        public OpenAIResponseClient Client { get; set; }

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

            if (string.IsNullOrEmpty(Model))
            {
                throw new ArgumentException("Model name cannot be null or empty.", nameof(model));
            }

            EnableWebSearch = enableWebSearch;
            EnableComputerCalls = enableComputerCalls;
            FileSearchOptions = vectorDbOptions ?? FileSearchOptions;

            SetAndValidateApiKey(_apiKeyCredential);

            Options = _clientOptions == null ? new OpenAIClientOptions() : _clientOptions;

            Client = new(model: model, ApiKeyCredential, Options);
        }

        private void SetAndValidateApiKey(ApiKeyCredential? _apiKeyCredential = null)
        {
            ApiKeyCredential = _apiKeyCredential == null ? new ApiKeyCredential(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) : _apiKeyCredential;

            string apiKey = string.Empty;

            ApiKeyCredential?.Deconstruct(out apiKey);

            if (ApiKeyCredential == null || string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API Key cannot be null or empty. Try Setting Enviornment Variable OPENAI_API_KEY", nameof(_apiKeyCredential));
            }
        }

        public (List<ResponseItem>, ResponseCreationOptions) SetupOpenAIClient(List<ModelItem> messages, ModelResponseOptions options)
        {
            //Convert Input items here
            List<ResponseItem> responseItems = ConvertToProviderItems(messages).ToList();

            //Convert Options here
            ResponseCreationOptions responseCreationOptions = new ResponseCreationOptions();
            responseCreationOptions.Instructions = options.Instructions;

            
            if(options.ReasoningOptions != null)
            {
                responseCreationOptions.ReasoningOptions = new ResponseReasoningOptions();
                responseCreationOptions.ReasoningOptions.ReasoningEffortLevel = options.ReasoningOptions.EffortLevel == ModelReasoningEffortLevel.High
                    ? ResponseReasoningEffortLevel.High
                    : ResponseReasoningEffortLevel.Low;
            }

            //Convert Tools here
            foreach (BaseTool tool in options.Tools)
            {
                responseCreationOptions.Tools.Add(ResponseTool.CreateFunctionTool(
                    tool.ToolName,
                    tool.ToolDescription,
                    tool.ToolParameters,
                    tool.FunctionSchemaIsStrict)
                    );
            }

            if (EnableWebSearch)
            {
                responseCreationOptions.Tools.Add(ResponseTool.CreateWebSearchTool());
            }

            if (FileSearchOptions!=null)
            {
                responseCreationOptions.Tools.Add(ResponseTool.CreateFileSearchTool(
                    FileSearchOptions.VectorIDs,
                    FileSearchOptions.MaxResults,
                    FileSearchOptions.RankingOptions,
                    FileSearchOptions.Filters
                    ));
            }

            if (EnableComputerCalls)
            {
                Size screenSize = ComputerToolUtility.GetScreenSize();


                responseCreationOptions.Tools.Add(ResponseTool.CreateComputerTool(ComputerToolEnvironment.Windows, screenSize.Width, screenSize.Height));

                responseCreationOptions.TruncationMode = ResponseTruncationMode.Auto;
            }

            ResponseTextOptions responseTextOptions = new ResponseTextOptions();

            //Convert Text Format Here
            if (options.OutputFormat != null)
            {
                responseTextOptions.TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    options.OutputFormat.JsonSchemaFormatName,
                    options.OutputFormat.JsonSchema,
                    jsonSchemaIsStrict: true
                    );
            }

            responseCreationOptions.TextOptions = responseTextOptions;
            
            return (responseItems, responseCreationOptions);
        }

        public async Task<ModelResponse> HandleStreaming(AsyncCollectionResult<StreamingResponseUpdate> responseUpdates, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            ModelResponse ResponseOutput = new();
            ResponseOutput.Model = options.Model;
            ResponseOutput.OutputFormat = options.OutputFormat ?? null;
            ResponseOutput.OutputItems = new List<ModelItem>();

            //Convert to possible output types as streaming message is received.
            await foreach (StreamingResponseUpdate update in responseUpdates)
            {
                if (update is StreamingResponseOutputItemDoneUpdate finishedItem)
                {
                    ResponseOutput.OutputItems.Add(ConvertFromProviderItem(finishedItem.Item));
                }
                if (update is StreamingResponseOutputItemAddedUpdate newItem)
                {
                    //Do nothing
                }
                else if (update is StreamingResponseOutputItemDoneUpdate completedItem)
                {
                    //Do nothing
                }
                else if (update is StreamingResponseOutputTextDeltaUpdate deltaUpdate)
                {
                    streamingCallback?.Invoke($"{deltaUpdate.Delta}");
                }
                else if (update is StreamingResponseFunctionCallArgumentsDeltaUpdate deltaUpdateFuncArgs)
                {
                    streamingCallback?.Invoke($"{deltaUpdateFuncArgs.Delta}");
                }
                else if (update is StreamingResponseContentPartAddedUpdate newContent)
                {
                    //Do nothing
                }
                else if (update is StreamingResponseContentPartDoneUpdate completedContent)
                {
                    //Do nothing
                }
            }

            //Return results.
            return ResponseOutput;
        }

        public override async Task<ModelResponse> CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            (List<ResponseItem> responseItems, ResponseCreationOptions responseCreationOptions) = SetupOpenAIClient(messages, options);

            return await HandleStreaming(
                Client.CreateResponseStreamingAsync(responseItems, responseCreationOptions), 
                options, 
                streamingCallback);
        }

        public override async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            //Convert Model Items to OpenAI Response Items and Options
            (List<ResponseItem> responseItems, ResponseCreationOptions responseCreationOptions) = SetupOpenAIClient(messages, options);

            //Create Open Ai response
            OpenAIResponse response = await Client.CreateResponseAsync(responseItems, responseCreationOptions);

            //Convert the response back to Model
            List<ModelItem> ModelItems = ConvertFromProviderItems(response, responseItems).ToList();

            //Return results.
            return new ModelResponse(ModelItems, outputFormat: options.OutputFormat ?? null, messages);
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
