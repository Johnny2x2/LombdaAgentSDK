using LombdaAgentSDK.Agents.DataClasses;
using OpenAI.Responses;
using System.Collections;

namespace LombdaAgentSDK
{
#pragma warning disable OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public partial class OpenAIModelClient
    {
        //Convert OpenAI -> Model
        public List<ModelMessageContent> ConvertProviderContentToModelContent(List<ResponseContentPart> contentParts, MessageResponseItem item = null)
        {
            List<ModelMessageContent> messageContent = new List<ModelMessageContent>();

            foreach (ResponseContentPart content in contentParts)
            {
                switch (content.Kind)
                {
                    case OpenAI.Responses.ResponseContentPartKind.InputText:
                        messageContent.Add(new ModelMessageRequestTextContent(content.Text)); break;
                    case OpenAI.Responses.ResponseContentPartKind.OutputText:
                        if (item != null && item.Role.ToString().ToUpper() == "USER")
                        {
                            messageContent.Add(new ModelMessageUserResponseTextContent(content.Text));
                        }
                        else if (item != null && item.Role.ToString().ToUpper() == "ASSISTANT")
                        {
                            messageContent.Add(new ModelMessageAssistantResponseTextContent(content.Text));
                        }
                        else if (item != null && item.Role.ToString().ToUpper() == "SYSTEM")
                        {
                            messageContent.Add(new ModelMessageSystemResponseTextContent(content.Text));
                        }
                        else if (item != null && item.Role.ToString().ToUpper() == "DEVELOPER")
                        {
                            messageContent.Add(new ModelMessageDeveloperResponseTextContent(content.Text));
                        }
                        else
                        {
                            messageContent.Add(new ModelMessageAssistantResponseTextContent(content.Text));
                        }
                        break;
                    case OpenAI.Responses.ResponseContentPartKind.InputImage:
                        messageContent.Add(new ModelMessageImageFileContent(content.InputFileBytes, content.InputFilename)); break;
                    case OpenAI.Responses.ResponseContentPartKind.InputFile:
                        if (!string.IsNullOrEmpty(content.InputFileId))
                        {
                            messageContent.Add(new ModelMessageFileContent().CreateFileContentByID(content.InputFileId));
                        }
                        else
                        {
                            messageContent.Add(new ModelMessageFileContent(content.InputFilename, content.InputFileBytes));
                        }
                        break;
                    case OpenAI.Responses.ResponseContentPartKind.Refusal:
                        messageContent.Add(new ModelMessageRefusalContent(content.Refusal)); break;
                    case OpenAI.Responses.ResponseContentPartKind.Unknown: break;
                    default: break;
                }
            }

            return messageContent;
        }
        public ModelComputerCallItem ConvertToModelComputerCall(ComputerCallResponseItem computerCall)
        {
            ModelStatus status;
            switch (computerCall.Action.Kind)
            {
                case ComputerCallActionKind.Click:

                    switch (computerCall.Action.ClickMouseButton.GetValueOrDefault())
                    {
                        case ComputerCallActionMouseButton.Left:
                            return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionClick(
                                    computerCall.Action.ClickCoordinates.Value.X,
                                    computerCall.Action.ClickCoordinates.Value.Y,
                                    MouseButtons.LEFT)
                                );
                        case ComputerCallActionMouseButton.Right:
                            return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionClick(
                                    computerCall.Action.ClickCoordinates.Value.X,
                                    computerCall.Action.ClickCoordinates.Value.Y,
                                    MouseButtons.RIGHT)
                                );
                        case ComputerCallActionMouseButton.Wheel:
                            return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionClick(
                                    computerCall.Action.ClickCoordinates.Value.X,
                                    computerCall.Action.ClickCoordinates.Value.Y,
                                    MouseButtons.MIDDLE)
                                );
                        case ComputerCallActionMouseButton.Back:
                            return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionClick(
                                    computerCall.Action.ClickCoordinates.Value.X,
                                    computerCall.Action.ClickCoordinates.Value.Y,
                                    MouseButtons.BACK)
                                );
                        case ComputerCallActionMouseButton.Forward:
                            return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionClick(
                                    computerCall.Action.ClickCoordinates.Value.X,
                                    computerCall.Action.ClickCoordinates.Value.Y,
                                    MouseButtons.FORWARD)
                                );
                        default:
                            return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolAction()
                                );
                    }
                case ComputerCallActionKind.DoubleClick:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionDoubleClick(
                                    computerCall.Action.DoubleClickCoordinates.Value.X,
                                    computerCall.Action.DoubleClickCoordinates.Value.Y)
                                );
                case ComputerCallActionKind.Drag:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionDrag(
                                    computerCall.Action.DragPath[0].X,
                                    computerCall.Action.DragPath[0].Y,
                                    computerCall.Action.DragPath[1].X,
                                    computerCall.Action.DragPath[1].Y)
                                );
                case ComputerCallActionKind.KeyPress:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionKeyPress(computerCall.Action.KeyPressKeyCodes.ToList())
                                );
                case ComputerCallActionKind.Move:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionMove(computerCall.Action.MoveCoordinates.Value.X, computerCall.Action.MoveCoordinates.Value.Y)
                                );
                case ComputerCallActionKind.Screenshot:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionScreenShot()
                                );
                case ComputerCallActionKind.Scroll:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionScroll(computerCall.Action.ScrollVerticalOffset ?? 0, computerCall.Action.ScrollHorizontalOffset ?? 0)
                                );
                case ComputerCallActionKind.Type:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionType(computerCall.Action.TypeText)
                                );
                case ComputerCallActionKind.Wait:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolActionWait()
                                );
                default:
                    return new ModelComputerCallItem(
                                computerCall.Id,
                                computerCall.CallId,
                                computerCall.Status.ToString().TryParseEnum(out status) ? status : ModelStatus.Completed,
                                new ComputerToolAction()
                                );
            }
        }
        public ModelComputerCallOutputItem ConvertToModelComputerOutputItem(ComputerCallOutputResponseItem computerCallOutput)
        {
            return new ModelComputerCallOutputItem(
                computerCallOutput.Id,
                computerCallOutput.CallId,
                computerCallOutput.Status.ToString().TryParseEnum(out ModelStatus status) ? status : ModelStatus.Completed,
                new ModelMessageImageFileContent(ComputerResults[computerCallOutput.CallId].ImageUrl));
        }
        public IList<ModelItem> ConvertFromProviderItems(IEnumerable messages)
        {
            List<ModelItem> responseItems = new List<ModelItem>();

            foreach (ResponseItem item in messages)
            {
                responseItems.Add(ConvertFromProviderItem(item));
            }

            return responseItems;
        }
        public ModelItem ConvertFromProviderItem(ResponseItem item)
        {
            if (item is WebSearchCallResponseItem webSearchCall)
            {
                return new ModelWebCallItem(webSearchCall.Id, webSearchCall.Status
                    .ToString()
                    .TryParseEnum(out ModelWebSearchingStatus _status) ? _status : ModelWebSearchingStatus.Completed);
            }
            else if (item is FileSearchCallResponseItem fileSearchCall)
            {
                List<FileSearchCallContent> resultContents = new List<FileSearchCallContent>();

                foreach (FileSearchCallResult file in fileSearchCall.Results)
                {
                    Console.WriteLine($" {file.Filename}");
                    resultContents.Add(new FileSearchCallContent(file.FileId, file.Text, file.Filename, file.Score));
                }

                return new ModelFileSearchCallItem(fileSearchCall.Id,
                    fileSearchCall.Queries.ToList(),
                    fileSearchCall.Status
                        .ToString()
                        .TryParseEnum(out ModelStatus status) ? status : ModelStatus.Completed
                        ,
                    resultContents);
            }
            else if (item is FunctionCallResponseItem toolCall)
            {
                ModelStatus status = ConvertOpenAIStatus(toolCall.Status.ToString());

                return new ModelFunctionCallItem(
                    toolCall.Id,
                    toolCall.CallId,
                    toolCall.FunctionName,
                    status,
                    toolCall.FunctionArguments
                    );

            }
            else if (item is FunctionCallOutputResponseItem toolOutput)
            {
                ModelStatus status = ConvertOpenAIStatus(toolOutput.Status.ToString());

                return new ModelFunctionCallOutputItem(
                    toolOutput.Id,
                    toolOutput.CallId,
                    toolOutput.FunctionOutput,
                    status,
                    toolOutput.CallId
                    );
            }
            else if (item is ComputerCallResponseItem computerCall)
            {
                return ConvertToModelComputerCall(computerCall);
            }
            else if (item is ComputerCallOutputResponseItem computerOutput)
            {
                return ConvertToModelComputerOutputItem(computerOutput);
            }
            else if (item is ReasoningResponseItem reasoningItem)
            {
                //They changed the reasoning item to not have an content, so we just return the ID and encrypted content
                return new ModelReasoningItem(reasoningItem.Id, [reasoningItem.GetSummaryText(),]);
            }
            else if (item is MessageResponseItem message)
            {
                List<ModelMessageContent> messageContent = ConvertProviderContentToModelContent(message.Content.ToList(), message);

                ModelStatus status = ConvertOpenAIStatus(message.Status.ToString());

                return new ModelMessageItem(
                    message.Id,
                    message.Role.ToString(),
                    messageContent,
                    status
                    );
            }
            else
            {
                throw new ArgumentException($"Unknown ResponseItem type: {item.GetType().Name}", nameof(item));
            }
        }

        //Convert Model -> OpenAI
        public ComputerCallResponseItem ConvertToProviderComputerCall(ModelComputerCallItem computerCall)
        {
            ComputerCallStatus status;
            if (computerCall.Action is ComputerToolActionClick)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateClickAction(computerCall.Action.MoveCoordinates,
                        computerCall.Action.MouseButtonClick.ToString().TryParseEnum(out ComputerCallActionMouseButton button) ? button : ComputerCallActionMouseButton.Left),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionDoubleClick)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateDoubleClickAction(computerCall.Action.MoveCoordinates,
                        computerCall.Action.MouseButtonClick.ToString().TryParseEnum(out ComputerCallActionMouseButton button) ? button : ComputerCallActionMouseButton.Left),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionDrag)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateDragAction([computerCall.Action.StartDragLocation, computerCall.Action.MoveCoordinates]),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionKeyPress)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateKeyPressAction(computerCall.Action.KeysToPress),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionMove)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateMoveAction(computerCall.Action.MoveCoordinates),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionScreenShot)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateScreenshotAction(),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionScroll)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateScrollAction(computerCall.Action.MoveCoordinates, computerCall.Action.ScrollHorOffset, computerCall.Action.ScrollVertOffset),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionType)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateTypeAction(computerCall.Action.TypeText),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            else if (computerCall.Action is ComputerToolActionWait)
            {
                return new ComputerCallResponseItem(
                        computerCall.CallId,
                        ComputerCallAction.CreateWaitAction(),
                        new List<ComputerCallSafetyCheck>()
                        );
            }
            //probably should throw error
            throw new Exception("Cannot Convert model item to response item");
        }
        public ComputerCallOutputResponseItem ConvertComputerOutputToProviderItem(ModelComputerCallOutputItem computerCallOutput)
        {
            return ResponseItem.CreateComputerCallOutputItem(
                computerCallOutput.CallId,
                new List<ComputerCallSafetyCheck>(),
                computerCallOutput.ScreenShot.DataBytes,
                computerCallOutput.ScreenShot.MediaType);
        }
        public List<ResponseContentPart> ConvertModelContentToProviderContent(List<ModelMessageContent> contentParts)
        {
            List<ResponseContentPart> messageContent = new List<ResponseContentPart>();
            foreach (ModelMessageContent content in contentParts)
            {
                if (content is ModelMessageRequestTextContent request)
                {
                    messageContent.Add(ResponseContentPart.CreateInputTextPart(request.Text));
                }
                else if (content is ModelMessageResponseTextContent response)
                {
                    messageContent.Add(ResponseContentPart.CreateOutputTextPart(response.Text, new List<ResponseMessageAnnotation>()));
                }
                else if (content is ModelMessageImageFileContent image)
                {
                    messageContent.Add(ResponseContentPart.CreateInputImagePart(image.DataBytes, image.MediaType));
                }
                else if (content is ModelMessageImageUrlContent imageurl)
                {
                    messageContent.Add(ResponseContentPart.CreateInputImagePart(imageurl.ImageUrl));
                }
                else if (content is ModelMessageFileContent file)
                {
                    messageContent.Add(ResponseContentPart.CreateInputFilePart(file.DataBytes, file.ContentType.ToString(), file.FileName));
                }
                else if (content is ModelMessageRefusalContent refusal)
                {
                    messageContent.Add(ResponseContentPart.CreateRefusalPart(refusal.Text));
                }
            }
            return messageContent;
        }
        public IList<ResponseItem> ConvertToProviderItems(IEnumerable messages)
        {
            List<ResponseItem> responseItems = new List<ResponseItem>();

            foreach (ModelItem item in messages)
            {
                if (item is ModelWebCallItem webSearchCall)
                {
                    WebSearchCallStatus status = ConvertModelWebStatusToProviderWebCallStatus(webSearchCall.Status.ToString());

                    WebSearchCallResponseItem call = ResponseItem.CreateWebSearchCallItem();
                    //call.Status = status; Can not set this yet
                    //Maybe lets not report them in stream until i can get ID returned without modfying the API
                    //responseItems.Add(ResponseItem.CreateWebSearchCallItem());
                }
                else if (item is ModelFileSearchCallItem fileSearchCall)
                {
                    List<FileSearchCallResult> fileSearchCallResults = new List<FileSearchCallResult>();

                    foreach (FileSearchCallContent file in fileSearchCall.Results)
                    {
                        FileSearchCallResult fileResult = new FileSearchCallResult();
                        fileResult.FileId = file.FileId;
                        fileResult.Text = file.Text;
                        fileResult.Filename = file.Filename;
                        fileResult.Score = file.Score;
                        fileSearchCallResults.Add(fileResult);
                    }

                    responseItems.Add(ResponseItem.CreateFileSearchCallItem(fileSearchCall.Queries, fileSearchCallResults));
                }
                else if (item is ModelFunctionCallItem toolCall)
                {
                    FunctionCallStatus status = ConvertModelStatusToProviderFunctionCallStatus(toolCall.Status.ToString());

                    responseItems.Add(ResponseItem.CreateFunctionCallItem(
                        toolCall.CallId,
                        toolCall.FunctionName,
                        toolCall.FunctionArguments
                        ));

                }
                else if (item is ModelFunctionCallOutputItem toolOutput)
                {
                    FunctionCallOutputResponseItem functionOutput = ResponseItem.CreateFunctionCallOutputItem(
                        toolOutput.CallId,
                        toolOutput.FunctionOutput
                        );

                    responseItems.Add(ResponseItem.CreateFunctionCallOutputItem(
                        toolOutput.CallId,
                        toolOutput.FunctionOutput
                        ));

                }
                else if (item is ModelReasoningItem reasoningItem)
                {
                    List<ReasoningSummaryPart> summaryParts = new List<ReasoningSummaryPart>();

                    foreach (var part in reasoningItem.Summary)
                    {
                        summaryParts.Add(ReasoningSummaryPart.CreateTextPart(part));
                    }
                    
                    responseItems.Add(ResponseItem.CreateReasoningItem(summaryParts));
                }
#pragma warning disable OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                else if (item is ModelComputerCallItem computerCall)
                {
                    responseItems.Add(ConvertToProviderComputerCall(computerCall));
                }
                else if (item is ModelComputerCallOutputItem computerOutput)
                {
                    responseItems.Add(ConvertComputerOutputToProviderItem(computerOutput));
                }
                else if (item is ModelMessageItem message)
                {
                    List<ResponseContentPart> messageContent = ConvertModelContentToProviderContent(message.Content);

                    ModelStatus status = ConvertOpenAIStatus(message.Status.ToString());
                    if (message.Role.ToUpper() == "ASSISTANT")
                    {
                        responseItems.Add(ResponseItem.CreateAssistantMessageItem(messageContent));
                    }
                    else if (message.Role.ToUpper() == "USER")
                    {
                        responseItems.Add(ResponseItem.CreateUserMessageItem(messageContent));
                    }
                    else if (message.Role.ToUpper() == "SYSTEM")
                    {
                        responseItems.Add(ResponseItem.CreateSystemMessageItem(messageContent));
                    }
                    else if (message.Role.ToUpper() == "DEVELOPER")
                    {
                        responseItems.Add(ResponseItem.CreateDeveloperMessageItem(messageContent));
                    }
                }
                else
                {
                    throw new ArgumentException($"Unknown ModelItem type: {item.GetType().Name}", nameof(messages));
                }
            }

            return responseItems;
        }

        //Enum Conversions
        public ModelWebSearchingStatus ConvertOpenAIWebsearchStatus(WebSearchCallStatus status)
        {
            switch (status)
            {
                case WebSearchCallStatus.InProgress: return ModelWebSearchingStatus.InProgress;
                case WebSearchCallStatus.Failed: return ModelWebSearchingStatus.Failed;
                case WebSearchCallStatus.Searching: return ModelWebSearchingStatus.Searching;
                case WebSearchCallStatus.Completed: return ModelWebSearchingStatus.Completed;
                default: return ModelWebSearchingStatus.Failed;
            }
        }

        public ModelStatus ConvertOpenAIStatus(string? status)
        {
            Enum.TryParse(typeof(ModelStatus), status, true, out object? result);
            return (ModelStatus)(result ?? ModelStatus.Incomplete);
        }

        public FunctionCallStatus ConvertModelStatusToProviderFunctionCallStatus(string? status)
        {
            Enum.TryParse(typeof(FunctionCallStatus), status, true, out object? result);
            return (FunctionCallStatus)(result ?? FunctionCallStatus.Incomplete);
        }

        public WebSearchCallStatus ConvertModelWebStatusToProviderWebCallStatus(string? status)
        {
            Enum.TryParse(typeof(WebSearchCallStatus), status, true, out object? result);
            return (WebSearchCallStatus)(result ?? WebSearchCallStatus.Failed);
        }

    }
#pragma warning restore OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
