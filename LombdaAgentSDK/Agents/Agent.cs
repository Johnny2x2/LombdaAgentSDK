using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

namespace LombdaAgentSDK.Agents
{
    /// <summary>
    /// Base Class to define agent behavior 
    /// </summary>
    public class Agent
    {
        /// <summary>
        /// Which provider client to use
        /// </summary>
        public ModelClient Client { get; set; }

        /// <summary>
        /// Response options for the run
        /// </summary>
        public ModelResponseOptions Options { get; set; } = new ModelResponseOptions();

        /// <summary>
        /// Name of the agent
        /// </summary>
        public string AgentName { get; }

        /// <summary>
        /// Instructions on how to process prompts
        /// </summary>
        public string Instructions  { get; set;}

        /// <summary>
        /// Model being used but Agent
        /// </summary>
        [Obsolete("Set Model in client this does nothing")]
        public string Model { get; set; }

        /// <summary>
        /// Data Type to Format response output as
        /// </summary>
        public Type? OutputSchema { get; set; }

        /// <summary>
        /// Tools available to the agent
        /// </summary>
        public List<Delegate>? Tools { get; set; } = new List<Delegate>();

        /// <summary>
        /// Map of function tools to their methods
        /// </summary>
        public Dictionary<string, FunctionTool> tool_list = new Dictionary<string, FunctionTool>();
        /// <summary>
        /// Map of agent tools to their agents
        /// </summary>
        public Dictionary<string, AgentTool> agent_tools = new Dictionary<string, AgentTool>();


        public Agent(ModelClient client, string _name, string _instructions = "", Type? _output_schema = null, List<Delegate>? _tools = null)
        {
            Client = client;
            AgentName = _name;
            Instructions = string.IsNullOrEmpty(_instructions) ? "You are a helpful assistant" : _instructions;
            Model = client.Model;
            OutputSchema = _output_schema;
            Tools = _tools ?? Tools;
            Options.Instructions = Instructions;


            if (OutputSchema != null)
            {
                Options.OutputFormat = OutputSchema.CreateJsonSchemaFormatFromType(true);
            }

            //Setup tools and agent tools
            if (Tools.Count > 0)
            {
                SetupTools(Tools);
            }
        }

        /// <summary>
        /// Setup the provided methods as tools
        /// </summary>
        /// <param name="Tools"></param>
        private void SetupTools(List<Delegate> Tools)
        {
            foreach (var fun in Tools)
            {
                //Convert Agent to tool
                if (fun.Method.Name.Equals("AsTool"))
                {
                    AgentTool? agentTool = (AgentTool?)fun.DynamicInvoke(); //Creates the Chat tool for the agents running as tools and adds them to global list
                                                                            //Add agent tool to context list
                    if (agentTool != null)
                    {
                        agent_tools.Add(agentTool.ToolAgent.AgentName, agentTool);
                        Options.Tools.Add(agentTool.Tool);
                    }
                }
                else
                {
                    //Convert Method to tool
                    FunctionTool? functionTool = fun.ConvertFunctionToTool();
                    if (functionTool != null)
                    {
                        tool_list.Add(functionTool.ToolName, functionTool);
                        Options.Tools.Add(functionTool);
                    }
                }
            }
        }

       
    }

}
