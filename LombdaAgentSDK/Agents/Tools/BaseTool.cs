namespace LombdaAgentSDK.Agents.Tools
{
    public class BaseTool
    {
        public string ToolName { get; set; }
        public string ToolDescription { get; set; }
        public BinaryData ToolParameters { get; set; }
        public bool FunctionSchemaIsStrict { get; set; }
        public BaseTool() { }

        public BaseTool(string toolName, string toolDescription, BinaryData toolParameters, bool strictSchema = false)
        {
            ToolName = toolName;
            ToolDescription = toolDescription;
            ToolParameters = toolParameters;
            FunctionSchemaIsStrict = strictSchema;
        }

        public virtual BaseTool CreateTool(string toolName, string toolDescription, BinaryData toolParameters, bool strictSchema = false)
        {
            return new BaseTool(toolName, toolDescription, toolParameters);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ToolAttribute : Attribute
    {
        private string description;
        private string[] in_parameters_description;

        public ToolAttribute()
        {

        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public string[] In_parameters_description { get => in_parameters_description; set => in_parameters_description = value; }
    }
}
