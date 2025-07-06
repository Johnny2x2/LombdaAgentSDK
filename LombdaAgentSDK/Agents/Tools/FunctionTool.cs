namespace LombdaAgentSDK.Agents.Tools
{
    public class FunctionTool :BaseTool
    {
        public Delegate Function { get; set; }
        public FunctionTool(string toolName, string toolDescription, BinaryData toolParameters, Delegate function, bool strictSchema = false)
            : base(toolName, toolDescription, toolParameters, strictSchema)
        {
            Function = function;
        }
    }
}
