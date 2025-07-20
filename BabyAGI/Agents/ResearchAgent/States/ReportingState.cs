using BabyAGI.Agents.ResearchAgent.DataModels;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LombdaAgentSDK.AgentStateSystem;

namespace BabyAGI.Agents.ResearchAgent.States
{
    class ReportingState : AgentState<string, ReportData>
    {
        public ReportingState(StateMachine stateMachine) : base(stateMachine){}

        public override Agent InitilizeStateAgent()
        {
            string instructions = """
                    You are a senior researcher tasked with writing a cohesive report for a research query.
                    you will be provided with the original query, and some initial research done by a research assistant.

                    you should first come up with an outline for the report that describes the structure and flow of the report. 
                    Then, generate the report and return that as your final output.

                    The final output should be in markdown format, and it should be lengthy and detailed. Aim for 5-10 pages of content, at least 1000 words.
                    """;

            return new Agent(new OpenAIModelClient("gpt-4o-mini"), "Reporting agent", instructions, _output_schema: typeof(ReportData));
        }

        public override async Task<ReportData> Invoke(string input)
        {
            return await BeginRunnerAsync<ReportData>(input);
        }
    }
}
