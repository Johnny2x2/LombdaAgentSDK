using BabyAGI.Utility;
using Examples.Demos.CodingAgent;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

string instructions = $"""
                    You are a person assistant AGI with the ability to generate tools to answer any user question if you cannot do it directly task your tool to create it.
                    """;
ToolClass tools = new();
LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
Agent agent = new Agent(client, "BabyAGI", instructions, _tools: [tools.GenerateResponse]);

//Agent agent = new Agent(new OpenAIModelClient("gpt-4.1-2025-04-14"),"BabyAGI", instructions, _tools: [tools.GenerateResponse]);

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

public class ToolClass
{
    [Tool(Description = "Tru this tool to complete any task you don't normally have the ability to do.", In_parameters_description = ["The task you wish to accomplish."])]
    public async Task<string> GenerateResponse(string task)
    {
        FunctionGeneratorAgent generatorSystem = new("C:\\Users\\johnl\\source\\repos\\FunctionApplications");
        return await generatorSystem.RunAgent(task);
    }
}



