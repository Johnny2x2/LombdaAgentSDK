using BabyAGI.Agents.ResearchAgent.DataModels;
using BabyAGI.Agents.ResearchAgent.States;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;

namespace BabyAGI.Agents.ResearchAgent
{
    public class ResearchAgent
    {
        [Tool(Description = "Use this tool for doing deep web research", In_parameters_description = ["The topic you wish to research."])]
        public async Task<string> DoResearch(string topic)
        {
            //Setup states
            PlanningState plannerState = new PlanningState(); //custom init so you can add input here
            ResearchState ResearchState = new ResearchState();
            ReportingState reportingState = new ReportingState();

            //Setup Transitions between states
            plannerState.AddTransition((result)=> result.items.Length > 0, ResearchState); //Check if a plan was generated or Rerun

            ResearchState.AddTransition(_ => true, reportingState); //Use Lambda expression For passthrough to reporting state

            reportingState.AddTransition(_ => true, new ExitState()); //Use Lambda expression For passthrough to Exit

            //Create State Machine Runner with String as input and ReportData as output
            StateMachine<string, ReportData> stateMachine = new();

            stateMachine.SetEntryState(plannerState);
            stateMachine.SetOutputState(reportingState);

            //Run the state machine
            List<ReportData> reports = await stateMachine.Run("topic");

            //Report on the last state with Results
            return reports[0].FinalReport;
        }
    }
}
