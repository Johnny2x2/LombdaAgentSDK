using BabyAGI;
using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Agents.ResearchAgent.DataModels;
using BabyAGI.FunctionGenerator.DataModels;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent.states;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.AgentStateSystem;
using NUnit.Framework;

namespace Examples.Demos.CodingAgent
{
    public class ToolCodingAgent : AgentStateMachine<FunctionBreakDownInput, CodeBuildInfoOutput>
    {
        public ToolCodingAgent(LombdaAgent lombdaAgent) : base(lombdaAgent) { }

        public override void InitilizeStates()
        {
            //Setup states
            CSharpCodingState codeState = new CSharpCodingState(this);//Program a solution
            CSharpBuildState buildState = new CSharpBuildState(this); //Execute a solution 
            CodeReviewerState reviewState = new CodeReviewerState(this);//How to fix the code
            FunctionEnricherState enricherState = new FunctionEnricherState(this);

            //Setup connections
            codeState.AddTransition(CheckProgramCreated, buildState);//Program a solution

            buildState.AddTransition(CheckProgramWorked, enricherState); //Executed a solution move to enricher
            buildState.AddTransition(reviewState); //If program fails

            reviewState.AddTransition(codeState); //How to fix the code

            enricherState.AddTransition(new ExitState()); //Exit path

            SetEntryState(codeState);
            SetOutputState(enricherState);
        }

        //Create validation functions
        public bool CheckProgramCreated(ProgramResultOutput result)
        {
            return result.Result.items.Length > 0;
        }

        public bool CheckProgramWorked(CodeBuildInfoOutput result)
        {
            CodeBuildInfo info = result.BuildInfo!;
            if (!info.BuildResult.BuildCompleted) { return false; }
            return true;
        }
    }


}
