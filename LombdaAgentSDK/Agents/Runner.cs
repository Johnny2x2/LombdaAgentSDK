using System.Drawing.Imaging;
using System.Text.Json;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using LlmTornado.Common;

namespace LombdaAgentSDK
{
    public class Runner
    {
        public delegate void ComputerActionCallbacks(ComputerToolAction computerCall);
        public delegate void RunnerVerboseCallbacks(string runnerAction);
        public delegate void StreamingCallbacks(string streamingResult);
        public static async Task<RunResult> RunAsync(
            Agent agent,
            string input = "",
            GuardRailFunction? guard_rail = null, 
            bool single_turn = false, 
            int maxTurns = 10,
            List<ModelItem>? messages = null,
            ComputerActionCallbacks computerUseCallback = null,
            RunnerVerboseCallbacks verboseCallback = null,
            bool streaming = false,
            StreamingCallbacks streamingCallback = null
            )
        {
            RunResult runResult = new RunResult();

            //Setup the messages from previous runs or memory
            if (messages != null) 
            {
                runResult.Messages.AddRange(messages);
            }

            //Add the latest message to the stream
            runResult.Messages.Add(new ModelMessageItem(Guid.NewGuid().ToString(), "USER", [new ModelMessageRequestTextContent(input),], ModelStatus.Completed));

            //Check if the input triggers a guardrail to stop the agent from continuing
            if (guard_rail != null)
            {
                var guard_railResult = await (Task<GuardRailFunctionOutput>)guard_rail.DynamicInvoke([input])!;
                if (guard_railResult != null) {
                    GuardRailFunctionOutput grfOutput = guard_railResult;
                    if (grfOutput.TripwireTriggered) throw new Exception($"Input Guardrail Stopped the agent from continuing because, {grfOutput.OutputInfo}");
                }
            }

            //Agent loop
            int currentTurn = 0;
            runResult.Response.OutputItems = new List<ModelItem>();

            do
            {
                if (currentTurn >= maxTurns) throw new Exception("Max Turns Reached");

                runResult.Response = await _get_new_response(agent, runResult.Messages, streaming, streamingCallback) ?? runResult.Response;

                currentTurn++;

            } while (await ProcessOutputItems(agent, runResult, verboseCallback, computerUseCallback));
            //Add output guardrail eventually
            
            return runResult;
        }

        public static async Task<bool> ProcessOutputItems(Agent agent, RunResult runResult, RunnerVerboseCallbacks callback, ComputerActionCallbacks computerUseCallback)
        {
            bool requiresAction = false;

            List<ModelItem> outputItems = runResult.Response.OutputItems!.ToList();

            foreach (ModelItem item in outputItems)
            {
                runResult.Messages.Add(item);

                await HandleVerboseCallback(item, callback);

                //Process Action Call
                if (item is ModelFunctionCallItem toolCall)
                {
                    runResult.Messages.Add(await HandleToolCall(agent, toolCall));

                    requiresAction = true;
                }
                else if (item is ModelComputerCallItem computerCall)
                {
                    runResult.Messages.Add(await HandleComputerCall(computerCall, computerUseCallback));

                    requiresAction = true;
                }
            }

            return requiresAction;
        }
        
        private static async Task<ModelFunctionCallOutputItem> HandleToolCall(Agent agent, ModelFunctionCallItem toolCall)
        {
            return agent.agent_tools.ContainsKey(toolCall.FunctionName) ? await ToolRunner.CallAgentToolAsync(agent, toolCall) : await ToolRunner.CallFuncToolAsync(agent, toolCall);
        }

        private static async Task HandleVerboseCallback(ModelItem item, RunnerVerboseCallbacks callback)
        {
            if (item is ModelWebCallItem webSearchCall)
            {
                callback?.Invoke($"[Web search invoked]({webSearchCall.Status}) {webSearchCall.Id}");
            }
            else if (item is ModelFileSearchCallItem fileSearchCall)
            {
                callback?.Invoke($"[File search invoked]({fileSearchCall.Status}) {fileSearchCall.Id}");
            }
            else if (item is ModelFunctionCallItem toolCall)
            {
                callback?.Invoke($"""
                        Calling tool:{toolCall.FunctionName}
                        using parameters:{JsonDocument.Parse(toolCall.FunctionArguments).RootElement.GetRawText()}
                        """);
            }
            else if (item is ModelComputerCallItem computerCall)
            {
                callback?.Invoke($"[Computer Call invoked]({computerCall.Status}) {computerCall.Action.TypeText}");
            }
            else if (item is ModelMessageItem message)
            {
                callback?.Invoke($"[Message]({message.Role}) {message.Text}");
            }
        }

        public static async Task<ModelResponse>? _get_new_response(Agent agent, List<ModelItem> messages, bool Streaming = false, StreamingCallbacks streamingCallback = null)
        {
            try
            {
                if(Streaming)
                {
                    return await agent.Client._CreateStreamingResponseAsync(messages, agent.Options, streamingCallback);
                }

                return await agent.Client._CreateResponseAsync(messages, agent.Options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Removing Last Message thread");
                RemoveLastMessageThread(messages);
            }

            return null;
        }

        public static async Task<ModelComputerCallOutputItem> HandleComputerCall(ModelComputerCallItem computerCall, ComputerActionCallbacks computerCallbacks = null)
        {
            computerCallbacks?.Invoke(computerCall.Action);

            Thread.Sleep(1000);

            byte[] data = ComputerToolUtility.TakeScreenshotByteArray(ImageFormat.Png);

            GC.Collect();

            return new ModelComputerCallOutputItem(Guid.NewGuid().ToString(), computerCall.CallId, ModelStatus.Completed, new ModelMessageImageFileContent(BinaryData.FromBytes(data), "image/png"));
        }

        /// <summary>
        /// Try to rerun last message thread if it fails.
        /// </summary>
        /// <param name="messages"></param>
        public static void RemoveLastMessageThread(List<ModelItem> messages)
        {
            //Remove last messages
            if (messages.Count > 1)
            {
                if (messages[messages.Count - 1] is ModelFunctionCallOutputItem || messages[messages.Count - 1] is ModelComputerCallOutputItem)
                {
                    messages.RemoveAt(messages.Count - 1); //Remove last input
                    messages.RemoveAt(messages.Count - 1); //Remove last input
                }
                else
                {
                    messages.RemoveAt(messages.Count - 1); //Remove last input
                }
            }
        }
    }
}
