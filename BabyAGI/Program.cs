using Examples.Demos.CodingAgent;
using Examples.Demos.FunctionGenerator.States;

string userInput = "Can you divide 2 numbers for me?";

FunctionBreakDownResults breakDownResults = await new BreakDownTaskState().Invoke(userInput);

List<Task> tasks = new();

foreach (var function in breakDownResults.FunctionsToGenerate)
{
    tasks.Add(Task.Run(async () => await RunAgent(function)));
}

await Task.WhenAll(tasks);

async Task RunAgent(FunctionBreakDown function)
{
    CSHARP_CodingAgent codeAgent = new CSHARP_CodingAgent();
    await codeAgent.RunCodingAgent(new FunctionBreakDownInput(userInput, function));
}
