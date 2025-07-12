using Examples.Demos.CodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.FunctionGenerator.States
{
    public struct FunctionFoundResult
    {
        
        public string FunctionName { get; set; }
        public bool FunctionFound { get; set; }
        public FunctionFoundResult(string functionName, bool functionFound) { FunctionName = functionName;  FunctionFound = functionFound; }
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

    public class CheckExistingFunctionState : BaseState<string, FunctionFoundResultOutput>
    {
        public async override Task<FunctionFoundResultOutput> Invoke(string args)
        {
            string instructions = string.Format("""
            You are an expert software assistant. The user has provided the following request:

            "{0}"

            Below is a list of available functions with their descriptions:

            {1}

            Determine if any of the existing functions perfectly fulfill the user's request. If so, return the name of the function.

            Provide your answer in the following JSON format:
            {{
                "FunctionFound": true or false,
                "FunctionName": "<name of the function if found, else null>"
            }}

            Examples:

            Example 1:
            User input: "Calculate the sum of two numbers"
            Functions: [{{"name": "add_numbers", "description": "Adds two numbers"}}]
            Response:
            {{
                "FunctionFound": true,
                "FunctionName": "add_numbers"
            }}

            Example 2:
            User input: "Translate text to French"
            Functions: [{{"name": "add_numbers", "description": "Adds two numbers"}}]
            Response:
            {{
                "FunctionFound": false,
                "FunctionName": null
            }}

            Now, analyze the user's request and provide the JSON response.
            """, args, string.Empty);

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(client,
                "Code Assistant",
                "You are an expert software assistant.",
                _tools: [CSHARP_CodingAgent.ReadFileTool, CSHARP_CodingAgent.GetFilesTool],
                _output_schema: typeof(FunctionFoundResult));

            RunResult result = await Runner.RunAsync(agent, instructions);

            return new FunctionFoundResultOutput(args, result.ParseJson<FunctionFoundResult>());
        }
        
    }



}
