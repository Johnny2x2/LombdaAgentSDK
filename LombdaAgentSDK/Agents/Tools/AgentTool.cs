namespace LombdaAgentSDK.Agents.Tools
{
    /// <summary>
    /// Run agent as a tool
    /// </summary>
    public class AgentTool
    {
        /// <summary>
        /// Agent to run
        /// </summary>
        public Agent ToolAgent { get; set; }
        /// <summary>
        /// Generated Schema of tool
        /// </summary>
        public BaseTool Tool { get; set; }

        public AgentTool(Agent agent, BaseTool tool)
        {
            ToolAgent = agent;
            Tool = tool;
        }
    }

}
