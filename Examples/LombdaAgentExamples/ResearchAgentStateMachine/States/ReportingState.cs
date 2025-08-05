using LombdaAgentSDK.Agents;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;

namespace Examples.LombdaAgentExamples.ResearchAgentStateMachine
{
    class ReportingState : AgentState<string, ReportData>
    {
        public ReportingState(StateMachine stateMachine) : base(stateMachine){}

        public override Agent InitilizeStateAgent()
        {
            return new Agent(
                client:new OpenAIModelClient("gpt-4o-mini"), 
                _name:"Reporting agent",
                _instructions:"""
                    You are a senior researcher tasked with writing a cohesive report for a research query.
                    you will be provided with the original query, and some initial research done by a research assistant.

                    you should first come up with an outline for the report that describes the structure and flow of the report. 
                    Then, generate the report and return that as your final output.

                    The final output should be in markdown format, and it should be lengthy and detailed. Aim for 5-10 pages of content, at least 1000 words.
                    """, 
                _output_schema: typeof(ReportData)
                );
        }

        public override async Task<ReportData> Invoke(string input)
        {
            return await BeginRunnerAsync<ReportData>(input);
        }
    }
}
