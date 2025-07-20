using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.FunctionGenerator.States;
using BabyAGI.Utility;
using Examples.Demos.FunctionGenerator.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.DataModels
{
    public struct FinalResult
    {
        public string AssistantMessage { get; set; }
        public string FinalResultSummary { get; set; }
    }
    public class FunctionExecutionResult
    {
        public ExecutableOutputResult ExecutionResult { get; set; }
        public FunctionFoundResultOutput functionFoundResultOutput { get; set; }

        public CommandLineArgs generatedArgs { get; set; }
        public FunctionExecutionResult(ExecutableOutputResult executionResult, FunctionFoundResultOutput functionFoundResultOutput, CommandLineArgs generatedArgs)
        {
            ExecutionResult = executionResult;
            this.functionFoundResultOutput = functionFoundResultOutput;
            this.generatedArgs = generatedArgs;
        }
    }

    public struct FunctionResultReview
    {
        public bool HasEnoughInformation { get; set; }
        public string InformationSummary { get; set; }
    }

    public class FunctionResultReviewOutput
    {
        public FunctionResultReview functionResultReview { get; set; }
        public FunctionExecutionResult functionExecutionResult { get; set; }
        public FunctionResultReviewOutput() { }
        public FunctionResultReviewOutput(FunctionExecutionResult functionResult, FunctionResultReview review)
        {
            functionResultReview = review;
            functionExecutionResult = functionResult;
        }
    }
    public struct FunctionFoundResult
    {
        public string FunctionName { get; set; }
        public bool FunctionFound { get; set; }
        public FunctionFoundResult(string functionName, bool functionFound) { FunctionName = functionName; FunctionFound = functionFound; }
    }

    public struct FunctionFoundResultOutput
    {
        public string UserInput { get; set; }
        public FunctionFoundResult FoundResult { get; set; }
        public FunctionFoundResultOutput(string userInput, FunctionFoundResult functionFound)
        {
            UserInput = userInput;
            FoundResult = functionFound;
        }
    }
    public struct FunctionBreakDownResults
    {
        public FunctionBreakDown[] FunctionsToGenerate { get; set; }
    }

    public struct FunctionBreakDown
    {
        public string FunctionName { get; set; }
        public string Description { get; set; }
        public ParameterType[] MainInputParameters { get; set; }
        public ParameterType OutputParameter { get; set; }
        public string ProgramCode { get; set; }
    }

    public struct FunctionBreakDownInput
    {
        public string Context { get; set; }
        public FunctionBreakDown functionBreakDown { get; set; }

        public FunctionBreakDownInput() { }
        public FunctionBreakDownInput(string context, FunctionBreakDown _functionBreakDown)
        {
            Context = context;
            functionBreakDown = _functionBreakDown;
        }

        public override string ToString()
        {
            return $@" 
                    Name: {functionBreakDown.FunctionName}
                    Description: {functionBreakDown.Description}
                    Input parameters: {{{string.Join(",", functionBreakDown.MainInputParameters)}}}
                    Output parameters: {{{functionBreakDown.OutputParameter}}} ";
        }
    }



    public struct ParameterType
    {
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }

        public override string ToString()
        {
            return $@"{{ ""Name"":""{name}"",""description"":""{description}"",""type"":""{type}""}}";
        }
    }
}
