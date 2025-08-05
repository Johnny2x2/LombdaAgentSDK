using BabyAGI.FunctionGenerator.States;
using Examples.Demos.FunctionGenerator.States;
using LombdaAgentSDK.AgentStateSystem;
using BabyAGI.FunctionGenerator.DataModels;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;

namespace Examples.Demos.FunctionGenerator
{
    public class FunctionGeneratorAgent : AgentStateMachine<string, FinalResult>
    {
        public string OriginalTask = "";
        public List<string> SavedResults = new List<string>();

        public FunctionGeneratorAgent(LombdaAgent lombdaAgent, string functionsPath) : base(lombdaAgent)
        {
            RuntimeProperties.TryAdd("FunctionsPath", functionsPath);
            RuntimeProperties.TryAdd("OriginalTask", OriginalTask);
            RuntimeProperties.TryAdd("SavedResults", SavedResults);
        }

        public override void InitilizeStates()
        {
            //Setup States
            CheckExistingFunctionState checkExistingFunctionState = new(this); // This state checks if a function already exists for the task 
            BreakDownTaskState breakDownTaskState = new(this); // This state breaks down the task into smaller subtasks if no function is found
            GenerateFunctionState generateFunctionState = new(this); // This state generates a new function based on the task or subtask
            FunctionExecutorState functionExecutorState = new(this); // This state executes the function found or generated for the task or subtask
            ResultReviewerState resultReviewerState = new(this); // This state reviews the results of the function execution to determine if they are sufficient
            ResultWriterState resultWriterState = new(this); // This state writes the results to a file or database if they are sufficient

            //Setup transitions
            //Found function to run
            checkExistingFunctionState.AddTransition((result) => result.FoundResult.FunctionFound, functionExecutorState);
            //No function found, need to break down task
            checkExistingFunctionState.AddTransition((result) => !result.FoundResult.FunctionFound, breakDownTaskState);

            //pass thru from breakdown task to generate function
            breakDownTaskState.AddTransition(generateFunctionState);

            //Passthrough to check the functions again
            generateFunctionState.AddTransition<string>((output) => OriginalTask, checkExistingFunctionState);

            //If execution completed , go to result reviewer
            functionExecutorState.AddTransition((result)=>result.ExecutionResult.ExecutionCompleted, resultReviewerState);

            //If enough information, go to result writer
            resultReviewerState.AddTransition((result) => result.functionResultReview.HasEnoughInformation, resultWriterState);

            //If not enough information, go back to check existing function with a summary of the information needed
            resultReviewerState.AddTransition((result) => !result.functionResultReview.HasEnoughInformation, (output) => output.functionResultReview.InformationSummary, checkExistingFunctionState);

            //If result writer is done, exit the state machine
            resultWriterState.AddTransition(new ExitState());

            SetEntryState(checkExistingFunctionState);
            SetOutputState(resultWriterState);
        }
    }
}
