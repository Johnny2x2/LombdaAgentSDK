using BabyAGI.Agents;
using BabyAGI.Agents.ResearchAgent;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static LombdaAgentSDK.Runner;

namespace BabyAGI
{
    public class BabyAGIRunner
    {
        public string MainThreadId { get; set; }
        public StreamingCallbacks StreamingCallback { get; set; }
        public Action<string> LoggingCallbacks { get; set; }
        public async Task RunAGI()
        {
            
            // Seems to require a little persuasion to get to use tool instead of openAI being like nah I can't do that.
            //Stop the process with EXIT()
            ComputerControllerAgent computerTool = new ComputerControllerAgent(this);
            ResearchAgent researchTool = new ResearchAgent();
            SimpleWebSearchAgent websearchTool = new SimpleWebSearchAgent();

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
            string instructions = $"""You are a person assistant AGI with the ability to generate tools to answer any user question if you cannot do it directly task your tool to create it.""";
            Agent agent = new Agent(client, "BabyAGI", instructions, _tools: [AttemptToCompleteTask, computerTool.ControlComputer, researchTool.DoResearch, websearchTool.BasicWebSearch]);

            Console.WriteLine("Enter a Message");
            Console.Write("[User]: ");
            string userInput = Console.ReadLine() ?? "";
            RunResult result = await Runner.RunAsync(agent, userInput, streaming: true, streamingCallback: StreamingCallback);
            MainThreadId = result.Response.Id;
            Console.WriteLine("");
            Console.Write("[User]: ");
            userInput = Console.ReadLine() ?? "";
            while (!userInput.Equals("EXIT()"))
            {
                result = await Runner.RunAsync(agent, userInput, messages: result.Messages, streaming: true, streamingCallback: StreamingCallback, responseID: MainThreadId);
                MainThreadId = result.Response.Id;
                Console.WriteLine("");
                Console.Write("[User]: ");
                userInput = Console.ReadLine() ?? "";
            }
        }
        [Tool(Description = "Use this before telling a user you are unable to do something", In_parameters_description = ["The task you wish to accomplish."])]
        public async Task<string> AttemptToCompleteTask(string task)
        {
            FunctionGeneratorAgent generatorSystem = new(BabyAGIConfig.FunctionsPath);
            return await generatorSystem.RunAgent(task);
        }

    }

}
