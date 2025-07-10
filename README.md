# LombdaAgentSDK

⚡ **LombdaAgentSDK** is a lightweight C# SDK designed to create and run modular "agents" that can execute tasks, manage state, and communicate with your custom infrastructure. Inspired by modern AI/automation agent patterns, it provides a framework for orchestrating workflows and modular logic with minimal overhead.

---

## 🚀 Features

* ✅ Simple `Agent` and `State` abstractions for building agent workflows.
* ⚙️ Support for state transitions, condition checks, and results.
* 🔍 Plug-and-play: easily inject your own function handlers.
* 📦 .NET Standard compatible – works across .NET Framework and .NET Core.

---

## 📂 Installation

Install with NuGet

```bash
dotnet add package LombdaAiAgents
```

Or include the library in your solution by adding the project reference.

---

## 🔧 Usage

### Run an Agent

```csharp
Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), "Assistant", "Have fun");

RunResult result = await Runner.RunAsync(agent, "Hello World!");
```

### Setup With LLMTornado

```csharp
LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                        [new ProviderAuthentication(LLmProviders.OpenAi, 
                        Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

Agent agent = new Agent(client, "Assistant", "Have fun");

RunResult result = await Runner.RunAsync(agent, "Hello World!");
```

### Automatic Structured Output from Type
```csharp
public struct math_step
{
    public string explanation { get; set; }
    public string output { get; set; }
}

Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Assistant", 
    "Have fun",
    _output_schema: typeof(math_step));

RunResult result = await Runner.RunAsync(agent, "How can I solve 8x + 7 = -23?");

//Helper function to extract json from last message
math_step mathResult = result.ParseJson<math_step>();
```

### Simple Tool Use
```csharp
void async Task Run()
{
    Agent agent = new Agent(
        new OpenAIModelClient("gpt-4o-mini"), 
        "Assistant", 
        "Have fun",  
        _tools : [GetCurrentWeather]);

    RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");

    Console.WriteLine(result.Text);
}

[Tool( Description = "Get the current weather in a given location",
       In_parameters_description = [
        "The city and state, e.g. Boston, MA",
        "The temperature unit to use. Infer this from the specified location."
        ])]
public string GetCurrentWeather(string location, Unit unit = Unit.celsius)
{
    // Call the weather API here.
    return $"31 C";
} 
```

## Create Complex Agent Workflows

### Create a States

```csharp
class PlanningState : BaseState<string, WebSearchPlan>
{
    public override async Task<WebSearchPlan> Invoke(string input)
    {
        string instructions = """
            You are a helpful research assistant. 
            Given a query, come up with a set of web searches, to perform to best answer the query. 
            Output between 5 and 20 terms to query for. 
            """;

        Agent agent = new Agent(
            new OpenAIModelClient("gpt-4o-mini"), 
            "Assistant", 
            instructions, 
            _output_schema: typeof(WebSearchPlan));

        return (await Runner.RunAsync(agent, this.Input)).ParseJson<WebSearchPlan>();
    }
}
```
### Connect States Together

```csharp
PlanningState plannerState = new PlanningState(); 
ResearchState ResearchState = new ResearchState();
ReportingState reportingState = new ReportingState();

//Setup Transitions between states
plannerState.AddTransition(IfPlanCreated, ResearchState); //Check if a plan was generated or Rerun

ResearchState.AddTransition(_ => true, reportingState); //Use Lambda expression For passthrough to reporting state

reportingState.AddTransition(_ => true, new ExitState()); //Use Lambda expression For passthrough to Exit
```
### Run the StateMachine
```csharp
//Create State Machine Runner
StateMachine stateMachine = new StateMachine();

//Run the state machine
await stateMachine.Run(plannerState, "Research for me top 3 best E-bikes under $1500 for mountain trails");

//Report on the last state with Results
Console.WriteLine(reportingState.Output.FinalReport);
```

### Creating State Machines

States essentially transforms the Input into the Output

Where `FooState : BaseState<InputType, OutputType>`

Invoke(InputType input) Must Return the Output Type (Strongly Typed)

You can only Transition to a state where the Output of the current state is the Input to the next state

```csharp
class ConvertStringToIntState : BaseState<string, int>
{
    public override async Task<int> Invoke(string input)
    {
        return int.Parse(this.Input)
    }
}
```
You can build pipelines of states and let the agent transition between them based on the results.


---

## 🚦 Roadmap
* [ ] Add non async support for agent execution.
* [ ] Improve logging and diagnostics.

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you’d like to change.

---

## 📄 License

[MIT](LICENSE)

---

## 🙌 Acknowledgements

LlmTornado
OpenAI-C#


