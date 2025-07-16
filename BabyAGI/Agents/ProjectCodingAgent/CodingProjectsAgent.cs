using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.Agents.ProjectCodingAgent.states;
using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.States;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using Examples.Demos.CodingAgent.states;
using Examples.Demos.FunctionGenerator.States;
using Examples.Demos.ProjectCodingAgent.states;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;
using NUnit.Framework;

namespace Examples.Demos.ProjectCodingAgent
{
    public class CodingProjectsAgent
    {
        public string FunctionsPath = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";
        public string ProjectName = "";

        public CodingProjectsAgent(string functionsPath) 
        { 
            FunctionsPath = functionsPath;
        }

        public string Context {  get; set; }

        public async Task<ProgramApprovalResult> RunProjectCodingAgent(string context)
        {
            Context = context;

            //Setup states
            ProjectDesignState designState = new ProjectDesignState(this); //Design the project
            ProjectCodeTaskCreatorState taskGenerator = new ProjectCodeTaskCreatorState(); //Generate tasks
            ProjectManagerState taskManagerState = new ProjectManagerState(); // Manage tasks
            ProjectCodingState codeState = new ProjectCodingState(this);//Program a solution
            ProjectBuildState buildState = new ProjectBuildState(this); //Execute a solution 
            ProjectReviewerState reviewState = new ProjectReviewerState(this);//How to fix the code
            ProjectApprovalState approvalState = new ProjectApprovalState(this); //Approve the project

            //Setup connections
            //Design the project
            designState.AddTransition(taskGenerator); //Design the project and move to task generator

            //Generate the task
            taskGenerator.AddTransition(taskManagerState); //Generate tasks and move to task manager

            //Manage the tasks
            taskManagerState.AddTransition((task)=> task.TaskId != 0, codeState); //If there is a task move to code state
            taskManagerState.AddTransition(approvalState); //If no tasks available, generate new tasks

            //Code the project
            codeState.AddTransition(buildState);//Program a solution

            // Build the project
            buildState.AddTransition(
                (result) => result.BuildInfo.BuildResult!.BuildCompleted && result.BuildInfo.ExecutableResult!.ExecutionCompleted, 
                (result) => new TaskBreakdownResult(), //If program works, move to next task with empty new task required
                taskManagerState); //Executed a solution, now move to next task
            buildState.AddTransition(reviewState); //If program failed review the code

            //Review the failed code
            reviewState.AddTransition(codeState); //How to fix the code

            //Approve the project
            approvalState.AddTransition((result) => result.Approval.Approved, new ExitState()); //If project is approved exit DONE!!!
            approvalState.AddTransition((output) => new ProgramDesignResult() { ApprovalResult = output }, taskGenerator); //If project is not approved, go back to task manager

            //Setup manager
            StateMachine<string, ProgramApprovalResult> stateMachine = new();

            stateMachine.SetEntryState(designState);
            stateMachine.SetOutputState(approvalState);

            return (await stateMachine.Run(context))[0]!;
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

        [Tool(Description = "Use this tool to read files already written", In_parameters_description = ["file path of the file you wish to read."])]
        public string ReadFileTool(string filePath)
        {
            return FunctionGeneratorUtility.ReadProjectFile(FunctionsPath, ProjectName, filePath);
        }

        [Tool(Description = "Use this tool to get all the file paths in the project")]
        public string GetFilesTool()
        {
            return FunctionGeneratorUtility.GetProjectFiles(FunctionsPath, ProjectName);
        }
    }


}
