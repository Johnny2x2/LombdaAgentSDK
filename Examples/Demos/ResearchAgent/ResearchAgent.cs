using Examples.Demos.ResearchAgent.DataModels;
using Examples.Demos.ResearchAgent.States;
using LombdaAgentSDK.StateMachine;

namespace Examples.Demos.ResearchAgent
{
    public class ResearchAgent
    {
        //[Test]
        public async Task Run()
        {
            //Setup states
            PlanningState plannerState = new PlanningState(); //custom init so you can add input here
            ResearchState ResearchState = new ResearchState();
            ReportingState reportingState = new ReportingState();

            //Setup Transitions between states
            plannerState.AddTransition(IfPlanCreated, ResearchState); //Check if a plan was generated or Rerun

            ResearchState.AddTransition(_ => true, reportingState); //Use Lambda expression For passthrough to reporting state

            reportingState.AddTransition(_ => true, new ExitState()); //Use Lambda expression For passthrough to Exit

            //Create State Machine Runner with String as input and ReportData as output
            StateMachine<string, ReportData> stateMachine = new();

            stateMachine.SetEntryState(plannerState);
            stateMachine.SetOutputState(reportingState);

            //Run the state machine
            List<ReportData> reports = await stateMachine.Run("What are the top 3 mountain bikes for under $1500?");

            //Report on the last state with Results
            Console.WriteLine(reports[0].FinalReport);
        }

        //Create validation delegate functions
        public bool IfPlanCreated(WebSearchPlan result)
        {
            return result.items.Length > 0;
        }
    }
}
