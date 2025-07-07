using Examples.Demos.ResearchAgent.DataModels;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.ResearchAgent.States
{
    //Design the states
    class PlanningState : BaseState<string, WebSearchPlan>
    {
        int attempts = 0;
        int maxAttempts = 5;

        public PlanningState(string input) => this.Input = input;


        public override void EnterState(string? input)
        {
            attempts = 0; //Reset attempts if state was exited/ReEntered (StateMachine will just case Invoke if State Doesn't change)
        }

        public override async Task<WebSearchPlan> Invoke()
        {
            attempts++;
            if (attempts > maxAttempts)
            {
                CurrentStateMachine.End(); //Kill the process on max attempts
            }

            string instructions = """
                    You are a helpful research assistant. Given a query, come up with a set of web searches, 
                    to perform to best answer the query. Output between 5 and 20 terms to query for. 
                    """;

            Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), "Assistant", instructions, _output_schema: typeof(WebSearchPlan));

            return (await Runner.RunAsync(agent, this.Input)).ParseJson<WebSearchPlan>();
        }
    }
}
