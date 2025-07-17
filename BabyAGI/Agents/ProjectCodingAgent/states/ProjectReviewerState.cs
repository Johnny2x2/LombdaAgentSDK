using BabyAGI.Agents.ProjectCodingAgent.DataModels;
using BabyAGI.BabyAGIStateMachine.DataModels;
using Examples.Demos.CodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;

namespace Examples.Demos.ProjectCodingAgent.states
{
    public class ReviewHistoy
    {
        public string errorDetails { get; set; }
        public CodeReview codeReview { get; set; }
        public ReviewHistoy(string errorDetails, CodeReview codeReview)
        {
            this.errorDetails = errorDetails;
            this.codeReview = codeReview;
        }

        public override string ToString()
        {
            return $"""
                    Error Details: {errorDetails}
                    Code Review: {codeReview.ToString()}
                    """;
        }
    }

    class ProjectReviewerState : BaseState<CodeProjectBuildInfoOutput, TaskItem>
    {
        public CodingProjectsAgent StateAgent {  get; set; }
        public ProjectReviewerState(CodingProjectsAgent stateAgent)
        {
            StateAgent = stateAgent;
        }

        public List<ReviewHistoy> errorHistory { get; set; } = new();

        
        public override async Task<TaskItem> Invoke(CodeProjectBuildInfoOutput codeBuildInfo)
        {
            string instructions = $"""
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.

                    Current Project Name: {StateAgent.ProjectName}
                    
                    Currrent Project Task:
                    {codeBuildInfo.ProgramResult.CurrentTask.Description}
                    
                    Expected Task Outcome:
                    {codeBuildInfo.ProgramResult.CurrentTask.ExpectedOutcome}
                    
                    Task Success Criteria:
                    {codeBuildInfo.ProgramResult.CurrentTask.SuccessCriteria}
                    
                    Task Complexity: {codeBuildInfo.ProgramResult.CurrentTask.Complexity} 
                    """;

            LLMTornadoModelProvider client = new(   ChatModel.OpenAi.Gpt41.V41,
                                                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(
                client,
                "CodeReviewer",
                instructions,
                _tools: [StateAgent.ReadFileTool, StateAgent.GetFilesTool],
                _output_schema: typeof(CodeReview));

            string executableResults = "";
            if(codeBuildInfo.BuildInfo.ExecutableResult != null)
            {
                executableResults = $"""
                    Executable Output: {codeBuildInfo.BuildInfo.ExecutableResult.Output}

                    Executable Error: {codeBuildInfo.BuildInfo.ExecutableResult.Error}
                    """;
            }

            string ErrorDetails = $"""
                    Build Errors: {codeBuildInfo.BuildInfo.BuildResult.Error}
                    {codeBuildInfo.BuildInfo.BuildResult.Output}
                    
                    Input Args: {codeBuildInfo.ProgramResult.Result.Sample_EXE_Args}

                    Executable Results:
                    {executableResults}
                    """;

            string Prompt = $"""
                    {ErrorDetails}

                    Previous Code Reviews:
                    {string.Join("\n\n", errorHistory.Select(r => r.ToString()))}
                    """;

            
            RunResult result = await Runner.RunAsync(agent, Prompt, maxTurns:50);

            CodeReview review = result.ParseJson<CodeReview>();

            errorHistory.Add(new ReviewHistoy(ErrorDetails, review));

            return new TaskItem
            {
                TaskId = 1738,
                Description = review.ReviewSummary,
                ExpectedOutcome = review.ToString(),
                Dependencies = Array.Empty<string>(),
                Complexity = "Medium",
                SuccessCriteria = "N/A"
            };
        }

    }

    
}
