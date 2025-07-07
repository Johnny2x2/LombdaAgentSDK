using Examples.Demos.ResearchAgent.DataModels;
using Examples.Demos.ResearchAgent.States;
using LombdaAgentSDK.StateMachine;

namespace Examples.Demos.ResearchAgent
{
    public class ResearchAgent
    {
        [Test]
        public async Task Run()
        {
            //Setup states
            PlanningState plannerState = new PlanningState("Research for me top 3 best E-bikes under $1500 for mountain trails"); //custom init so you can add input here
            ResearchState ResearchState = new ResearchState();
            ReportingState reportingState = new ReportingState();

            //or Add input to first branch of state machine with Set Input
            plannerState.SetInput("Research for me top 3 best E-bikes under $1500 for mountain trails");

            //Setup Transitions between states
            plannerState.Transitions.Add(new StateTransition<WebSearchPlan>(IfPlanCreated, ResearchState)); //Check if a plan was generated or Rerun

            ResearchState.Transitions.Add(new StateTransition<string>(_ => true, reportingState)); //Use Lambda expression For passthrough to reporting state

            reportingState.Transitions.Add(new StateTransition<ReportData>(_ => true, new ExitState())); //Use Lambda expression For passthrough to Exit

            //Create State Machine Runner
            StateMachine stateMachine = new StateMachine();

            //Run the state machine
            await stateMachine.Run(plannerState);

            //Report on the last state with Results
            Console.WriteLine(reportingState.Output.FinalReport);
        }

        //Create validation delegate functions
        public bool IfPlanCreated(WebSearchPlan result)
        {
            return result.items.Length > 0;
        }
    }
}
