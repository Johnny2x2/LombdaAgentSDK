using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.Utility;
using Examples.Demos.CodingAgent.states;
using Examples.Demos.FunctionGenerator.States;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;
using NUnit.Framework;

namespace Examples.Demos.CodingAgent
{
    public class CSHARP_CodingAgent
    {
        public string FunctionsPath = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";
        public string ProjectName = "";
        public CSHARP_CodingAgent(string functionsPath) 
        { 
            FunctionsPath = functionsPath;
        }
        public FunctionBreakDownInput Context {  get; set; }
        public async Task<CodeBuildInfoOutput> RunCodingAgent(FunctionBreakDownInput context)
        {
            ProjectName = context.functionBreakDown.FunctionName;

            Context = context;

            FunctionGeneratorUtility.CreateNewProject(FunctionsPath, ProjectName, context.functionBreakDown.Description);

            //Setup states
            CSharpCodingState codeState = new CSharpCodingState(this);//Program a solution
            CSharpBuildState buildState = new CSharpBuildState(this); //Execute a solution 
            CodeReviewerState reviewState = new CodeReviewerState(this);//How to fix the code
            FunctionEnricherState enricherState = new FunctionEnricherState(this);

            //Setup connections
            codeState.AddTransition(CheckProgramCreated, buildState);//Program a solution

            buildState.AddTransition(CheckProgramWorked, enricherState); //Executed a solution move to enricher
            buildState.AddTransition(_ => true, reviewState); //If program fails

            reviewState.AddTransition(_ => true, codeState); //How to fix the code

            enricherState.AddTransition(_=> true, new ExitState()); //Exit path

            //Setup manager
            StateMachine<string, CodeBuildInfoOutput> stateMachine = new();

            stateMachine.SetEntryState(codeState);
            stateMachine.SetOutputState(enricherState);
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
