using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using Newtonsoft.Json;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LlmTornado.Common;

namespace LombdaAgentSDK
{
    public partial class LLMTornadoModelProvider : ModelClient
    {
        public TornadoApi Client { get; set; }
        public ChatModel CurrentModel { get; set; }

        public LLMTornadoModelProvider(
            ChatModel model, List<ProviderAuthentication> provider)
        {
            Model = model.Name;
            CurrentModel = model;
            Client = new TornadoApi(provider);
        }

        public LLMTornadoModelProvider(
            ChatModel model, Uri provider)
        {
            Model = model.Name;
            CurrentModel = model;
            Client = new TornadoApi(provider);
        }

        public LLMTornadoModelProvider(
            ChatModel model, TornadoApi client )
        {
            Model = model;
            CurrentModel = model;
            Client = client;
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
            Conversation chat = Client.Chat.CreateConversation(CurrentModel);

            chat = SetupClient(chat, messages, options);

            return await HandleStreaming(chat, messages, options, streamingCallback);
        }

        public override async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            Conversation chat = Client.Chat.CreateConversation(CurrentModel);

            chat =  SetupClient(chat, messages, options);

            //Create Open response
            RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();

            //Convert the response back to Model
            //A bit redundant I can cache the current Model items already converted and only process the new ones
            List<ModelItem> ModelItems = ConvertFromProviderItems(response.Data!, chat).ToList();

            //Return results.
            return new ModelResponse(options.Model, [ConvertLastFromProviderItems(chat),], outputFormat: options.OutputFormat ?? null, ModelItems);
        }
    }
}
