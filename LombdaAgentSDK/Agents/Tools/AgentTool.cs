namespace LombdaAgentSDK.Agents.Tools
{
    public class AgentTool
    {
        public Agent ToolAgent { get; set; }
        //Need to abstract this
        public BaseTool Tool { get; set; }

        public AgentTool(Agent agent, BaseTool tool)
        {
            ToolAgent = agent;
            Tool = tool;
        }
    }

}
