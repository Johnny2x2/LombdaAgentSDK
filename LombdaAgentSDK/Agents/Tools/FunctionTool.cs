﻿namespace LombdaAgentSDK.Agents.Tools
{
    /// <summary>
    /// Convert Methods into Agent tools
    /// </summary>
    public class FunctionTool : BaseTool
    {
        /// <summary>
        /// Function to Invoke this needs to be fixed to string return type
        /// </summary>
        public Delegate Function { get; set; }
        public FunctionTool(string toolName, string toolDescription, BinaryData toolParameters, Delegate function, bool strictSchema = false)
            : base(toolName, toolDescription, toolParameters, strictSchema)
        {
            Function = function;
        }
    }
}
