using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

namespace LombdaAgentSDK.Agents
{
    public class Agent
    {
        public ModelClient Client { get; set; }

        public ModelResponseOptions Options { get; set; } = new ModelResponseOptions();

        public string AgentName { get; }

        public string Instructions  { get; set;}

        public string Model { get; set; }

        public Type? OutputSchema { get; set; }

        public List<Delegate>? Tools { get; set; } = new List<Delegate>();

        public Dictionary<string, FunctionTool> tool_list = new Dictionary<string, FunctionTool>();
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

        public void SetupTools(List<Delegate> Tools)
        {
            foreach (var fun in Tools)
            {
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
