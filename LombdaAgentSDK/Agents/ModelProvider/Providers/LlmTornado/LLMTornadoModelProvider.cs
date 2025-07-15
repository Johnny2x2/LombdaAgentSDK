using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;
using LlmTornado.Threads;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using FunctionCall = LlmTornado.ChatFunctions.FunctionCall;

namespace LombdaAgentSDK
{
    public partial class LLMTornadoModelProvider : ModelClient
    {
        public TornadoApi Client { get; set; }
        public ChatModel CurrentModel { get; set; }

        public bool UseResponseAPI { get; set; }

        public bool AllowComputerUse { get; set; }
        public VectorSearchOptions? VectorSearchOptions { get; set; }
        public bool EnableWebSearch { get; set; } = false;

        //Need to add in some Converters first
        //public bool EnableCodeInterpreter { get; set; } = false;
        //Need MPC
        //Need LocalShell

        public LLMTornadoModelProvider(
            ChatModel model, List<ProviderAuthentication> provider, bool useResponseAPI = false, 
            bool allowComputerUse = false, VectorSearchOptions? searchOptions = null, bool enableWebSearch = false)
        {
            Model = model.Name;
            CurrentModel = model;
            Client = new TornadoApi(provider);
            UseResponseAPI = useResponseAPI;
            AllowComputerUse = allowComputerUse;
            VectorSearchOptions = searchOptions;
            EnableWebSearch = enableWebSearch;
        }

        public LLMTornadoModelProvider(
            ChatModel model, Uri provider, bool useResponseAPI = false, bool allowComputerUse = false)
        {
            Model = model.Name;
            CurrentModel = model;
            Client = new TornadoApi(provider);
            UseResponseAPI = useResponseAPI;
            AllowComputerUse = allowComputerUse;
        }

        public LLMTornadoModelProvider(
            ChatModel model, TornadoApi client, bool useResponseAPI = false, bool allowComputerUse = false)
        {
            Model = model;
            CurrentModel = model;
            Client = client;
            UseResponseAPI = useResponseAPI;
            AllowComputerUse = allowComputerUse;
        }

        public Conversation SetupClient(Conversation chat, List<ModelItem> messages, ModelResponseOptions options)
        {
            //Convert Tools here
            foreach (BaseTool tool in options.Tools)
            {
                if (chat.RequestParameters.Tools == null) chat.RequestParameters.Tools = new List<LlmTornado.Common.Tool>();
                chat.RequestParameters.Tools?.Add(
                    new LlmTornado.Common.Tool(
                        new LlmTornado.Common.ToolFunction(
                            tool.ToolName,
                            tool.ToolDescription,
                            tool.ToolParameters.ToString()),true
                        )
                    );
            }

            //Convert Text Format Here
            if (options.OutputFormat != null)
            {
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(options.OutputFormat.JsonSchema.ToString());
                chat.RequestParameters.ResponseFormat = ChatRequestResponseFormats.StructuredJson(options.OutputFormat.JsonSchemaFormatName, responseFormat);
            }

            if (options.ReasoningOptions != null)
            {
                chat.RequestParameters.ReasoningEffort = options.ReasoningOptions.EffortLevel switch
                {
                    ModelReasoningEffortLevel.Low => ChatReasoningEfforts.Low,
                    ModelReasoningEffortLevel.Medium => ChatReasoningEfforts.Medium,
                    ModelReasoningEffortLevel.High => ChatReasoningEfforts.High,
                    _ => ChatReasoningEfforts.Low
                };
            }

            chat = ConvertToProviderItems(messages, chat);

            return chat;
        }

        public ResponseRequest SetupResponseClient(List<ModelItem> messages, ModelResponseOptions options)
        {
            List<ResponseInputItem> InputItems = new();

            InputItems.AddRange(ConvertToProviderResponseItems(new List<ModelItem>([messages.Last()])));

            ResponseRequest request = new ResponseRequest
            {
                Model = CurrentModel,
                InputItems = InputItems,
                Instructions = options.Instructions
            };
            
            if (request.Tools == null) request.Tools = new List<ResponseTool>();

            request.PreviousResponseId = options.PreviousResponseId ?? null;
            //Convert Tools here
            foreach (BaseTool tool in options.Tools)
            {
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(tool.ToolParameters.ToString());

                ResponseFunctionTool rftool = new()
                {
                    Name = tool.ToolName,
                    Description = tool.ToolDescription,
                    Parameters = responseFormat,
                    Strict =tool.FunctionSchemaIsStrict
                };

                request.Tools.Add(rftool);
            }

            if(VectorSearchOptions != null)
            {
                request.Tools.Add(new ResponseFileSearchTool
                {
                    VectorStoreIds = VectorSearchOptions.VectorIDs
                });
            }

            if (AllowComputerUse)
            {
                Size screenSize = ComputerToolUtility.GetScreenSize();
                request.Tools.Add(new ResponseComputerUseTool
                {
                    DisplayWidth = screenSize.Width,
                    DisplayHeight = screenSize.Height,
                    Environment = ResponseComputerEnvironment.Windows
                });
                request.Truncation = ResponseTruncationStrategies.Auto;

                request.Reasoning = new ReasoningConfiguration()
                {
                    Summary = ResponseReasoningSummaries.Concise
                };
                request.Background = false;
            }

            //Convert Text Format Here
            if (options.OutputFormat != null)
            {
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(options.OutputFormat.JsonSchema.ToString());

                var config1 = ResponseTextFormatConfiguration.CreateJsonSchema(
                    responseFormat,
                    options.OutputFormat.JsonSchemaFormatName,
                    strict: true);

                request.Text = config1;
            }

            if (options.ReasoningOptions != null)
            {
                request.Reasoning = new ReasoningConfiguration()
                {
                    Effort = options.ReasoningOptions.EffortLevel switch
                    {
                        ModelReasoningEffortLevel.Low => ResponseReasoningEfforts.Low,
                        ModelReasoningEffortLevel.Medium => ResponseReasoningEfforts.Medium,
                        ModelReasoningEffortLevel.High => ResponseReasoningEfforts.High,
                        _ => ResponseReasoningEfforts.Low
                    }
                };
            }

            if(EnableWebSearch)
            {
                request.Tools.Add(new ResponseWebSearchTool());
            }

            //Need to add Convertion methods to model provider converts for ResponseConverter
            //if (EnableCodeInterpreter)
            //{
            //    request.Tools.Add(new ResponseCodeInterpreterTool());
            //}

            return request;
        }

        public async Task<ModelResponse> HandleStreaming(Conversation chat, List<ModelItem> messages, ModelResponseOptions options, Runner.StreamingCallbacks streamingCallback = null)
        {
            ModelResponse ResponseOutput = new();
            ResponseOutput.Model = options.Model;
            ResponseOutput.OutputFormat = options.OutputFormat ?? null;
            ResponseOutput.OutputItems = new List<ModelItem>();
            ResponseOutput.Messages = messages;

            //Create Open response
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = (text) =>
                {
                    streamingCallback?.Invoke(text);
                    return ValueTask.CompletedTask;
                },
                ReasoningTokenHandler = (reasoning) =>
                {
                    streamingCallback?.Invoke(reasoning.Content);
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (message) =>
                {
                    //Call the streaming callback for completion
                    ResponseOutput.OutputItems.Add(ConvertFromProviderItem(message));
                    return ValueTask.CompletedTask;
                },
                MessagePartHandler = (part) =>
                {
                    return ValueTask.CompletedTask;
                },
                FunctionCallHandler = (toolCall) =>
                {
                    foreach(FunctionCall call in toolCall)
                    {
                        streamingCallback?.Invoke($"INVOKING -> [{call.Name}]");
                        //Add the tool call to the response output
                        ResponseOutput.OutputItems.Add(new ModelFunctionCallItem(
                            call.ToolCall?.Id!,
                            call.ToolCall?.Id!,
                            call.Name,
                            ModelStatus.InProgress,
                            BinaryData.FromString(call.Arguments)
                            ));
                    }
                    return ValueTask.CompletedTask;
                }

            });

            return ResponseOutput;
        }

        public override async Task<ModelResponse> CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, Runner.StreamingCallbacks streamingCallback = null)
        {
            if (AllowComputerUse)
            {
                throw new Exception("Cannot Stream while using Computer Model, try verbose callbacks");
            }

            return UseResponseAPI ? await StreamingResponseAPIAsync(messages, options, streamingCallback) : await StreamingChatAPIAsync(messages, options, streamingCallback);
        }

        public async Task<ModelResponse> StreamingChatAPIAsync(List<ModelItem> messages, ModelResponseOptions options, Runner.StreamingCallbacks streamingCallback = null)
        {
            Conversation chat = Client.Chat.CreateConversation(CurrentModel);

            chat = SetupClient(chat, messages, options);

            return await HandleStreaming(chat, messages, options, streamingCallback);
        }

        public async Task<ModelResponse> StreamingResponseAPIAsync(List<ModelItem> messages, ModelResponseOptions options, Runner.StreamingCallbacks streamingCallback = null)
        {
            ResponseRequest request = SetupResponseClient(messages, options);

            ModelResponse ResponseOutput = new();
            ResponseOutput.Model = options.Model;
            ResponseOutput.OutputFormat = options.OutputFormat ?? null;
            ResponseOutput.OutputItems = new List<ModelItem>();
            ResponseOutput.Messages = messages;

            await Client.Responses.StreamResponseRich(request, new ResponseStreamEventHandler
            {
                OnEvent = (data) =>
                {
                    if (data is ResponseEventOutputTextDelta delta)
                    {
                        streamingCallback?.Invoke(delta.Delta);
                    }

                    if (data is ResponseEventOutputItemDone itemDone)
                    {
                        ResponseOutput.OutputItems.Add(ConvertFromProviderOutputItem(itemDone.Item));

                        if (itemDone.Item is ResponseFunctionToolCallItem call)
                        {
                            streamingCallback?.Invoke($"INVOKING -> [{call.Name}]");
                        }
                    }

                    return ValueTask.CompletedTask;
                }
            });

            return ResponseOutput;
        }

        public override async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            return UseResponseAPI ? await CreateFromResponseAPIAsync(messages, options) : await CreateFromChatAPIAsync(messages, options);
        }

        public async Task<ModelResponse> CreateFromChatAPIAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            Conversation chat = Client.Chat.CreateConversation(CurrentModel);

            chat = SetupClient(chat, messages, options);

            //Create Open response
            RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();

            //Convert the response back to Model
            //A bit redundant I can cache the current Model items already converted and only process the new ones
            List<ModelItem> ModelItems = ConvertFromProviderItems(response.Data!, chat).ToList();

            //Return results.
            return new ModelResponse(options.Model, [ConvertLastFromProviderItems(chat),], outputFormat: options.OutputFormat ?? null, ModelItems);
        }

        public async Task<ModelResponse> CreateFromResponseAPIAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            ResponseRequest request = SetupResponseClient(messages, options);

            ResponseResult response = await Client.Responses.CreateResponse(request);

            List<ModelItem> ModelItems = ConvertOutputItems(response.Output);

            options.PreviousResponseId = response.Id;

            return new ModelResponse(ModelItems, outputFormat: options.OutputFormat ?? null, messages, id: response.Id);
        }
    }
}
