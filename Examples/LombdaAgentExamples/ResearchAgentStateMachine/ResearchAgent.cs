using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;

namespace Examples.LombdaAgentExamples.ResearchAgentStateMachine
{
    public class ResearchAgent : AgentStateMachine<string, ReportData>
    {
        public ResearchAgent(LombdaAgent lombdaAgent) : base(lombdaAgent) { }

        public override void InitilizeStates()
        {
            //Setup states
            PlanningState plannerState = new PlanningState(this); //custom init so you can add input here
            ResearchState ResearchState = new ResearchState(this);
            ReportingState reportingState = new ReportingState(this);

            //Setup Transitions between states
            plannerState.AddTransition((result) => result.items.Length > 0, ResearchState); //Check if a plan was generated or Rerun
            ResearchState.AddTransition(reportingState); //Use Lambda expression For passthrough to reporting state
            reportingState.AddTransition(new ExitState()); //Use Lambda expression For passthrough to Exit

            //Create State Machine Runner with String as input and ReportData as output
            SetEntryState(plannerState);
            SetOutputState(reportingState);
        }
    }
}
