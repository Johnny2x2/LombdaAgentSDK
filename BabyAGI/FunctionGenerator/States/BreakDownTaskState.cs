using Examples.Demos.CodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.FunctionGenerator.States
{
    public struct FunctionBreakDownResults
    {
        public FunctionBreakDown[] FunctionsToGenerate {  get; set; }
    }

    public struct FunctionBreakDown 
    { 
        public string FunctionName { get; set; }
        public string Description { get; set; }
        public ParameterType[] MainInputParameters {  get; set; }
        public ParameterType OutputParameter { get; set; }
        public string ProgramCode { get; set; }
    }

    public struct FunctionBreakDownInput
    {
        public string Context { get; set; }
        public string FunctionName { get; set; }
        public string Description { get; set; }
        public ParameterType[] MainInputParameters { get; set; }
        public ParameterType OutputParameter { get; set; }
        public string ProgramCode { get; set; }

        public FunctionBreakDownInput() { }
        public FunctionBreakDownInput(string context, string functionName, string description, ParameterType[] inputTypes, ParameterType OutputType, string code) 
        {
            Context = context;
            FunctionName = functionName;
            Description = description;
            MainInputParameters = inputTypes;
            OutputParameter = OutputType;
            ProgramCode = code;
        }

        public override string ToString()
        {
            return $@" 
                    Name: {FunctionName}
                    Description: {Description}
                    Input parameters: {{{string.Join(",", MainInputParameters)}}}
                    Output parameters: {{{OutputParameter}}} ";
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

    public class BreakDownTaskState:BaseState<FunctionFoundResultOutput, FunctionBreakDownResults>
    {
        private const string Format = @"
            You are an expert software assistant helping to break down a user's request into smaller functions for a microservice-inspired architecture. 
            The system is designed to be modular, with each function being small and designed optimally for potential future reuse.
            

            When breaking down the task, consider the following:

            >Each function should be as small as possible and do one thing well.
            >Use existing functions where possible. You have access to functions such as 'gpt_call', 'find_similar_function', and others in our function database.
            >Functions can depend on each other. Use 'dependencies' to specify which functions a function relies on.
            >Functions should include appropriate 'imports' if external libraries are needed.
            >Provide the breakdown as a list of functions, where each function includes its 'name', 'description', 'input_parameters', 'output_parameters', 'dependencies', and 'code' (just a placeholder or brief description at this stage).
            >Make sure descriptions are detailed so an engineer could build it to spec.
            >Every sub function you create should be designed to be reusable by turning things into parameters, vs hardcoding them.

            User request:

            {0}

            Provide your answer in JSON format as a list of functions. Each function should have the following structure:

            {{ ""FunctionName"": ""function_name"",
              ""Description"": ""Brief description of the function"",
              ""InputParameters"": [{{""name"": ""param1"", ""type"": ""type1"", ""description"": ""param1 description""}}, ...],
              ""OutputParameter"": {{""name"": ""output"", ""type"": ""type""}},
              ""code"": ""Placeholder or brief description"" 
            }}

            Example:

            [
              {{
                  ""FunctionName"": ""process_data"",
                  ""Description"": ""Processes input data"",
                  ""InputParameters"": [{{""name"": ""data"", ""type"": ""str"", ""description"": ""data to process""}}],
                  ""OutputParameter"": {{""name"": ""processed_data"", ""type"": ""str""}},
                  ""code"": ""Placeholder for process_data function""
              }},
              {{
                  ""FunctionName"": ""analyze_data"",
                  ""Description"": ""Analyzes processed data"",
                  ""InputParameters"": [{{""name"": ""processed_data"", ""type"": ""str"", ""description"": ""data that was processed""}}],
                  ""OutputParameter"": {{""name"": ""analysis_result"", ""type"": ""str"", , ""description"": ""Results from analysis""}},
                  ""code"": ""Placeholder for analyze_data function""
              }}
            ]

            Now, provide the breakdown for the user's request.";

        public async override Task<FunctionBreakDownResults> Invoke(FunctionFoundResultOutput input)
        {
            string instructions = string.Format(Format, input.UserInput);

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(client,
                "Code Assistant",
                "You are an expert software assistant.",
                _output_schema: typeof(FunctionBreakDownResults));

            RunResult result = await Runner.RunAsync(agent, instructions);
            FunctionBreakDownResults bdResult = result.ParseJson<FunctionBreakDownResults>();
            return bdResult;
        }
    }
}
