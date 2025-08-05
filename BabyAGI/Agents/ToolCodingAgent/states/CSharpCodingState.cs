using Examples.Demos.FunctionGenerator.States;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.CodingAgent.states
{
    //Design the states
    class CSharpCodingState : AgentState<string, ProgramResultOutput>
    {
        public CSharpCodingState(StateMachine stateMachine) : base(stateMachine)
        {

        }

        public override async Task<ProgramResultOutput> Invoke(string input)
        {
            string prompt = @"You are an expert C# programmer. Your task is to write detailed and working code for the following function based on the context provided. 
                    The C# Console Program will be Built and executed later from a .exe and will use the input args to get the function to work.
                    Make sure Program code is the main entry to utilize .net8.0 args structure when executing exe
                    Do not provide placeholder code, but rather do your best like you are the best senior engineer in the world and provide the best code possible. DO NOT PROVIDE PLACEHOLDER CODE.
                    Overall context:
                    {0}";

            ProgramResult program = await BeginRunnerAsync<ProgramResult>(string.Format(prompt, input));

            return new ProgramResultOutput(program, input);
        }

        public override Agent InitilizeStateAgent()
        {
            LLMTornadoModelProvider client = new(
            ChatModel.OpenAi.Gpt41.V41Mini,
            [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            return new Agent(client,
                    "Code Assistant",
                    "You are an expert C# programmer. Your task is to write detailed and working code for the following function based on the context provided.",
                    _output_schema: typeof(ProgramResult));
        }
    }
}
