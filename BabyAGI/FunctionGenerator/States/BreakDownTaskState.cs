using BabyAGI.FunctionGenerator.DataModels;
using Examples.Demos.CodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.FunctionGenerator.States
{

    public class BreakDownTaskState: AgentState<FunctionFoundResultOutput, FunctionBreakDownResults>
    {
        private const string instructions = @"
            You are an expert software assistant helping to break down a user's request into smaller functions for a microservice-inspired architecture. 
            The system is designed to be modular, with each function being small and designed optimally for potential future reuse.
            

            When breaking down the task, consider the following:

            >Each function should be as small as possible and do one thing well.
            >Provide the breakdown as a list of functions, where each function includes its 'name', 'description', 'input_parameters', 'output_parameters', 'dependencies', and 'code' (just a placeholder or brief description at this stage).
            >Make sure descriptions are detailed so an engineer could build it to spec.
            >Every sub function you create should be designed to be reusable by turning things into parameters, vs hardcoding them.


            Each function should have the following structure:

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

        public BreakDownTaskState(StateMachine stateMachine) : base(stateMachine) { }

        public override Agent InitilizeStateAgent()
        {
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(client,
                "Assistant",
                instructions,
                _output_schema: typeof(FunctionBreakDownResults));
        }

        public async override Task<FunctionBreakDownResults> Invoke(FunctionFoundResultOutput input)
        {
            return await BeginRunnerAsync<FunctionBreakDownResults>(input.UserInput);
        }
    }
}
