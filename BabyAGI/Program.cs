using BabyAGI;
using BabyAGI.Agents;
using BabyAGI.Agents.ResearchAgent;
using BabyAGI.BabyAGIStateMachine.Memory;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ProgramRun programRun = new ProgramRun();
        await programRun.Run();
    }


    public class ProgramRun
    {
        public string LatestResponse { get; set; } = string.Empty;

        public async Task Run()
        {
            LLMTornadoModelProvider _modelProvider = new LLMTornadoModelProvider(
                   ChatModel.OpenAi.Gpt41.V41Mini,
                   [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"))],
                   true);

            var agent = new Agent(
               _modelProvider,
               "Assistant",
               "You are a helpful assistant with access to various tools",
               _tools: [GetWeather, CalculateArea, GetCurrentTime]);

            agent.Options.AllowParallelToolCalling = true; // Allow parallel tool calls
            agent.ToolPermissionRequired["GetCurrentTime"] = true; // Require permission for GetCurrentTime tool
            // Act
            var result = await Runner.RunAsync(agent, "What time is it and the weather in boston?");

            Console.WriteLine($"Result: {result.Text}");
        }

        bool ToolPermission(string message)
        {
            Console.WriteLine(message);
            var response = Console.ReadLine();
            if (response?.ToLower() == "yes")
            {
                return true;
            }
            else if (response?.ToLower() == "no")
            {
                return false;
            }
            else
            {
                Console.WriteLine("Invalid response, please enter 'yes' or 'no'.");
                return ToolPermission(message);
            }
        }

        string ResponseReceived(string message)
        {
            Console.WriteLine(message);
            var response = Console.ReadLine();
            return response ?? "Error";
        }

        // Test tools for integration testing
        [Tool(Description = "Get weather information for a city")]
        public static string GetWeather(string city)
        {
            return $"The weather in {city} is sunny and 72°F";
        }

        [Tool(Description = "Calculate the area of a rectangle")]
        public static double CalculateArea(double width, double height)
        {
            return width * height;
        }

        [Tool(Description = "Get the current time")]
        public static string GetCurrentTime()
        {
            return $"The current time is {DateTime.Now:HH:mm:ss}";
        }

    }
    
}
