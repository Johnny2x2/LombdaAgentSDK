using LlmTornado.Common;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;

namespace LombdaAgentSDK
{
    /// <summary>
    /// Class to Invoke the tools during run
    /// </summary>
    public static class ToolRunner
    {
        /// <summary>
        /// Invoke function from FunctionCallItem and return FunctionOutputItem
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<ModelFunctionCallOutputItem> CallFuncToolAsync(Agent agent, ModelFunctionCallItem call)
        {
            List<object> arguments = new();

            if (!agent.tool_list.TryGetValue(call.FunctionName, out FunctionTool? tool))
                throw new Exception($"I don't have a tool called {call.FunctionName}");

            if (!json_util.IsValidJson(call.FunctionArguments.ToString()))
                throw new System.Text.Json.JsonException($"Function arguments for {call.FunctionName} are not valid JSON");

            //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
            if (call.FunctionArguments != null)
            {
                arguments = tool.Function.ParseFunctionCallArgs(call.FunctionArguments) ?? new();
            }

            var _result = await CallFuncAsync(tool.Function, [.. arguments]);


            return new ModelFunctionCallOutputItem("fc_" + Guid.NewGuid().ToString().Replace("-", "_"), call.CallId, _result?.ToString() ?? "ERROR", call.Status, call.FunctionName);
        }


        public static async Task<ModelFunctionCallOutputItem> CallMcpToolAsync(Agent agent, ModelFunctionCallItem call)
        {
            List<object> arguments = new();

            if (!agent.mcp_tools.TryGetValue(call.FunctionName, out MCPServer? server))
                throw new Exception($"I don't have a tool called {call.FunctionName}");

            var json = call.FunctionArguments.ToString();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json);
            var _result = await server.McpClient!.CallToolAsync(call.FunctionName,dict);

            if (_result is CallToolResult callToolResult)
            {
                string? result = "";

                if (callToolResult.Content.Count > 0)
                {
                    ContentBlock firstBlock = callToolResult.Content[0];

                    switch (firstBlock)
                    {
                        case TextContentBlock textBlock:
                            {
                                result = textBlock.Text;
                                break;
                            }
                        case ImageContentBlock imageBlock:
                            {
                                result = imageBlock.Data;
                                break;
                            }
                        case AudioContentBlock audioBlock:
                            {
                                result = audioBlock.Data;
                                break;
                            }
                        case EmbeddedResourceBlock embeddedResourceBlock:
                            {
                                result = embeddedResourceBlock.Resource.Uri;
                                break;
                            }
                        case ResourceLinkBlock resourceLinkBlock:
                            {
                                result = resourceLinkBlock.Uri;
                                break;
                            }
                    }
                }
                return new ModelFunctionCallOutputItem("fc_" + Guid.NewGuid().ToString().Replace("-", "_"), call.CallId, result, call.Status, call.FunctionName);
            }


            return new ModelFunctionCallOutputItem("fc_" + Guid.NewGuid().ToString().Replace("-", "_"), call.CallId, "ERROR", call.Status, call.FunctionName);
        }


        /// <summary>
        /// Invoke Agent from FunctionCallItem and return FunctionOutputItem
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<ModelFunctionCallOutputItem> CallAgentToolAsync(Agent agent, ModelFunctionCallItem call)
        {
            if (!agent.agent_tools.TryGetValue(call.FunctionName, out AgentTool? tool))
                throw new Exception($"I don't have a Agent tool called {call.FunctionName}");

            Agent newAgent = tool.ToolAgent;
            if (call.FunctionArguments != null)
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(call.FunctionArguments);

                if (argumentsJson.RootElement.TryGetProperty("input", out JsonElement jValue))
                {
                    RunResult agentToolResult = await Runner.RunAsync(newAgent, jValue.GetString());
                    return new ModelFunctionCallOutputItem("fc_" + Guid.NewGuid().ToString().Replace("-", "_"), call.CallId, agentToolResult.Text ?? "Could not get function result", call.Status, call.FunctionName);
                }
            }

            return new ModelFunctionCallOutputItem(Guid.NewGuid().ToString(), call.CallId, string.Empty, call.Status, call.FunctionName);
        }

        /// <summary>
        /// Handles the actual Method Invoke async/syncr and returns the result
        /// </summary>
        /// <param name="function"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static async Task<object?> CallFuncAsync(Delegate function, object[] args)
        {
            object? result;
            MethodInfo method = function.Method;
            if (AsyncHelpers.IsGenericTask(method.ReturnType, out Type taskResultType))
            {
                // Method is async, invoke and await
                var task = (Task)function.DynamicInvoke(args);
                await task.ConfigureAwait(false);
                // Get the Result property from the Task
                result = taskResultType.GetProperty("Result").GetValue(task);
            }
            else
            {
                // Method is synchronous
                result = function.DynamicInvoke( args);
            }

            return result ?? null;
        }

    }
}
