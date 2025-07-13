using BabyAGI.FunctionGenerator.States;
using Examples.Demos.FunctionGenerator.States;
using LlmTornado.Responses;
using LombdaAgentSDK.StateMachine;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.FunctionGenerator
{
    public class FunctionGeneratorAgent
    {
        public string FunctionsPath = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";
        public string OriginalTask = "";
        public List<string> SavedResults = new List<string>();

        public FunctionGeneratorAgent(string functionsPath)
        {
            FunctionsPath = functionsPath;
        }
        
        public async Task<string> RunAgent(string task)
        {
            OriginalTask = task;

            //Setup States
            CheckExistingFunctionState checkExistingFunctionState = new(this); 
            BreakDownTaskState breakDownTaskState = new();
            GenerateFunctionState generateFunctionState = new(this);
            FunctionExecutorState functionExecutorState = new(this);
            ResultReviewerState resultReviewerState = new(this);
            ConverterState<FunctionResultReviewOutput, string> reviewConverter = new((input) => input.functionResultReview.InformationSummary);
            ConverterState<FunctionBreakDownResults, string> generatorConverter = new((input) => OriginalTask);
            ResultWriterState resultWriterState = new(this);

            //Setup transitions
            checkExistingFunctionState.AddTransition((result) => result.FoundResult.FunctionFound, functionExecutorState);
            checkExistingFunctionState.AddTransition((result) => !result.FoundResult.FunctionFound, breakDownTaskState);

            breakDownTaskState.AddTransition(_ => true, generateFunctionState);

            generateFunctionState.AddTransition(_=> true, generatorConverter);
            generatorConverter.AddTransition(_=> true, checkExistingFunctionState);

            functionExecutorState.AddTransition((result)=>result.ExecutionResult.ExecutionCompleted, resultReviewerState);

            resultReviewerState.AddTransition((result) => result.functionResultReview.HasEnoughInformation, resultWriterState);
            resultReviewerState.AddTransition((result) => !result.functionResultReview.HasEnoughInformation, reviewConverter);
            reviewConverter.AddTransition(_ => true, checkExistingFunctionState);

            resultWriterState.AddTransition(_=>true, new ExitState());

            StateMachine<string, FinalResult> stateMachine = new();

            stateMachine.SetEntryState(checkExistingFunctionState);
            stateMachine.SetOutputState(resultWriterState);

            var result = await stateMachine.Run(task);

            return result[0].AssistantMessage;
        }
    }
}
