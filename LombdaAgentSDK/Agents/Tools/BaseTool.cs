namespace LombdaAgentSDK.Agents.Tools
{
    /// <summary>
    /// Base tool class for converting provider tools
    /// </summary>
    public class BaseTool
    {
        /// <summary>
        /// Name of the tool
        /// </summary>
        public string ToolName { get; set; }
        /// <summary>
        /// Description of the tool
        /// </summary>
        public string ToolDescription { get; set; }
        /// <summary>
        /// input Parameters of the tool
        /// </summary>
        public BinaryData ToolParameters { get; set; }
        /// <summary>
        /// Should all fields be required?
        /// </summary>
        public bool FunctionSchemaIsStrict { get; set; } = false;
        public BaseTool() { }

        public BaseTool(string toolName, string toolDescription, BinaryData toolParameters, bool strictSchema = false)
        {
            ToolName = toolName;
            ToolDescription = toolDescription;
            ToolParameters = toolParameters;
            FunctionSchemaIsStrict = strictSchema;
        }
        /// <summary>
        /// Create new tool based off the provided schema
        /// </summary>
        /// <param name="toolName"></param>
        /// <param name="toolDescription"></param>
        /// <param name="toolParameters"></param>
        /// <param name="strictSchema"></param>
        /// <returns></returns>
        public virtual BaseTool CreateTool(string toolName, string toolDescription, BinaryData toolParameters, bool strictSchema = false)
        {
            return new BaseTool(toolName, toolDescription, toolParameters);
        }
    }
    /// <summary>
    /// Attribute for assigning function as tool 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ToolAttribute : Attribute
    {
        /// <summary>
        /// Description of the function
        /// </summary>
        private string description;

        /// <summary>
        /// Description of the input parameters (must include all)
        /// </summary>
        private string[] in_parameters_description;

        /// <summary>
        /// Description of the function
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        /// <summary>
        /// Description of the input parameters (must include all)
        /// </summary>
        public string[] In_parameters_description { get => in_parameters_description; set => in_parameters_description = value; }


        public ToolAttribute()
        {

        }
    }
}
