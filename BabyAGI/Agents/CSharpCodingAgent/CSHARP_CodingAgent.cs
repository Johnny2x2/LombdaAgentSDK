using Examples.Demos.CodingAgent.states;
using Examples.Demos.FunctionGenerator;
using Examples.Demos.FunctionGenerator.States;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;
using NUnit.Framework;

namespace Examples.Demos.CodingAgent
{
    public partial class CSHARP_CodingAgent
    {
        public string FunctionsPath = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";
        public string ProjectName = "";


        [Test]
        public async Task TestCodingAgent()
        {
            await RunCodingAgent(new FunctionBreakDownInput(
                "Can you add 2 numbers for me?",
                "Adding2NumbersFunction",
                "Function to add 2 numbers together", 
                [new ParameterType() { name = "A", description="First Number", type= "int"}, new ParameterType() { name = "B", description = "Second Number", type = "int" }],
                new ParameterType() { name = "Result", description = "Output of the 2 numbers being added", type = "int" },
                "Code to add 2 numbers in .net 8.0 Console Application int.parse(args[0])+int.parse(args[1])"
                ));
        }

        public async Task<string> RunCodingAgent(FunctionBreakDownInput context)
        {
            ProjectName = context.FunctionName;

            FunctionGeneratorUtility.CreateNewProject(FunctionsPath, ProjectName, context.Description);

            //Setup states
            CSharpCodingState codeState = new CSharpCodingState(this);//Program a solution
            CSharpBuildState buildState = new CSharpBuildState(this); //Execute a solution 
            CodeReviewerState reviewState = new CodeReviewerState(this);//How to fix the code

            //Setup connections
            codeState.AddTransition(CheckProgramCreated, buildState);//Program a solution

            buildState.AddTransition(CheckProgramWorked, new ExitState()); //Execute a solution Exit path
            buildState.AddTransition(_ => true, reviewState); //If program fails

            reviewState.AddTransition(_ => true, codeState); //How to fix the code

            //Setup manager
            StateMachine<string, string> stateMachine = new();

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
