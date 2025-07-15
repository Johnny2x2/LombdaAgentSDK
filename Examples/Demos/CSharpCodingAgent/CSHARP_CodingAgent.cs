using Examples.Demos.CodingAgent.states;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;
using static Examples.Demos.CodeUtility;

namespace Examples.Demos.CodingAgent
{
    public class CSHARP_CodingAgent
    {
        public static string ProjectBuildPath = "C:\\Users\\jlomba\\source\\repos\\ConsoleTesting";
        public static string ProjectName = "ConsoleTesting";

        //[Test]
        public async Task Run()
        {
            FileIOUtility.SafeWorkingDirectory =  Path.Combine(CSHARP_CodingAgent.ProjectBuildPath, ProjectName);

            //Setup states
            CSharpCodingState codeState = new CSharpCodingState();//Program a solution
            CSharpBuildState buildState = new CSharpBuildState(); //Execute a solution 
            CodeReviewerState reviewState = new CodeReviewerState();//How to fix the code

            string input = """
                Make me a .net8.0 C# Class LIB for a state machine that can transition to multiple states and async run those and collect the data for the next state. Use the existing code to assist in creation.
                """;

            //Setup connections
            codeState.AddTransition(CheckProgramCreated, buildState);//Program a solution

            buildState.AddTransition(CheckProgramWorked, new ExitState()); //Execute a solution Exit path
            buildState.AddTransition(_ => true, reviewState); //If program fails

            reviewState.AddTransition(_ => true, codeState); //How to fix the code

            //Setup manager
            StateMachine stateMachine = new();

            await stateMachine.Run(codeState, input);
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
        public static string ReadFileTool(string filePath)
        {
            return FileIOUtility.ReadFile(filePath);
        }

        [Tool(Description = "Use this tool to get all the file paths in the project")]
        public static string GetFilesTool()
        {
            return FileIOUtility.GetAllPaths(ProjectName);
        }
    }


}
