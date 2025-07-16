using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Agents.ProjectCodingAgent.states;
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

        public FunctionBreakDownInput Context {  get; set; }

        public async Task<CodeBuildInfoOutput> RunProjectCodingAgent(FunctionBreakDownInput context)
        {
            ProjectName = context.functionBreakDown.FunctionName;

            Context = context;

            FunctionGeneratorUtility.CreateNewProject(FunctionsPath, ProjectName, context.functionBreakDown.Description);

            //Setup states
            ProjectCodeTaskCreatorState taskGenerator = new ProjectCodeTaskCreatorState();
            ProjectCodingState codeState = new ProjectCodingState(this);//Program a solution
            ProjectBuildState buildState = new ProjectBuildState(this); //Execute a solution 
            ProjectReviewerState reviewState = new ProjectReviewerState(this);//How to fix the code
            TaskManagerState taskManagerState = new TaskManagerState();

            taskGenerator.AddTransition(_ => true, taskManagerState);
            taskManagerState.AddTransition((task)=> task.TaskId != 0, codeState); //If there is a task move to code state
            taskManagerState.AddTransition(_ => true, new ExitState()); //If no tasks available, generate new tasks

            //Setup connections
            codeState.AddTransition(_=>true, buildState);//Program a solution

            buildState.AddTransition<TaskBreakdownResult>(
                (result) => result.BuildInfo.BuildResult.BuildCompleted && result.BuildInfo.ExecutableResult.ExecutionCompleted, 
                (result) => new TaskBreakdownResult(), //If program works, move to enricher
                taskManagerState); //Executed a solution move to next task

            buildState.AddTransition(
                (result) => !result.BuildInfo.BuildResult.BuildCompleted && !result.BuildInfo.ExecutableResult.ExecutionCompleted, 
                reviewState); //If program fails review the code

            reviewState.AddTransition(_ => true, codeState); //How to fix the code


            //Setup manager
            StateMachine<string, CodeBuildInfoOutput> stateMachine = new();

            stateMachine.SetEntryState(codeState);
            stateMachine.SetOutputState(buildState);

            string inputPrompt = $"User Request {context.Context} \n\n Function context: {context.ToString()}";

            return (await stateMachine.Run(inputPrompt))[0]!;
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
