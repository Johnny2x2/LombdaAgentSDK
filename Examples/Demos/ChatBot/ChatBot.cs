using Examples.Demos.OpenAIComputerUsePreview;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.ChatBot
{
    internal class ChatBot
    {
        public static void Main()
        {
            Task runner = Task.Run(async () => await StartDemo());

            Task.WaitAll(runner);
        }

        public static async Task StartDemo()
        {
            OpenAIModelClient openAIModelClient = new OpenAIModelClient("o4-mini-2025-04-16"); //Holding result cache to get the reasoning to work..

            Agent agent = new Agent(
                openAIModelClient,
                "Assistant",
                "You are a useful assistant."
                );

            RunResult result = new();

            string task = Console.ReadLine();

            while (!task.Equals("EXIT()"))
            {
                result = await RunChatBot(agent, task, result);
                task = Console.ReadLine();
            }
        }

        public static async Task<RunResult> RunChatBot(Agent agent, string task, RunResult? previousResult = null)
        {
            return await Runner.RunAsync(agent, task, messages: previousResult?.Messages, verboseCallback:Console.WriteLine);
        }
    }
}
