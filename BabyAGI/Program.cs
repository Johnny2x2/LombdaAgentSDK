using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

//Seems to require a little persuasion to get to use tool instead of openAI being like nah I can't do that.
//Stop the process with EXIT()
string instructions = $"""You are a person assistant AGI with the ability to generate tools to answer any user question if you cannot do it directly task your tool to create it.""";

BabyAGIConfig config = new();
LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
Agent agent = new Agent(client, "BabyAGI", instructions, _tools: [config.AttemptToCompleteTask]);

Console.WriteLine("Enter a Message");
Console.Write("[User]: ");
string userInput = Console.ReadLine() ?? "";
RunResult result = await Runner.RunAsync(agent, userInput, streaming: true, streamingCallback: Console.Write);
Console.WriteLine("");
Console.Write("[User]: ");
userInput = Console.ReadLine() ?? "";
while (!userInput.Equals("EXIT()"))
{
    result = await Runner.RunAsync(agent, userInput, messages:result.Messages, streaming:true, streamingCallback:Console.Write);
    Console.WriteLine("");
    Console.Write("[User]: ");
    userInput = Console.ReadLine() ?? "";
}

public class BabyAGIConfig
{
    public static string ChromaDbURI = "http://localhost:8001/api/v2/";
    public static string FunctionsPath = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";

    public BabyAGIConfig()
    {
        if (!Directory.Exists(FunctionsPath))
        {
            throw new Exception("Functions directory is not setup");
        }
    }

    [Tool(Description = "Try this tool to complete any task you don't normally have the ability to do.", In_parameters_description = ["The task you wish to accomplish."])]
    public async Task<string> AttemptToCompleteTask(string task)
    {
        FunctionGeneratorAgent generatorSystem = new(FunctionsPath);
        return await generatorSystem.RunAgent(task);
    }
}



