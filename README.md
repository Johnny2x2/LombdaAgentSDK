# LombdaAgentSDK

⚡ **LombdaAgentSDK** is a lightweight C# SDK designed to create and run modular "agents" that can execute tasks, manage state, and communicate with your custom infrastructure. Inspired by modern AI/automation agent patterns, it provides a framework for orchestrating workflows and modular logic with minimal overhead.

---
> [!CAUTION]
> This is a framework built by Johnny2x2 who has never held a job as a developer. 
> The purpose of this repo is to share ideas and spark discussion and for experienced devs to play with. Not meant for production use. Use with caution.

---
## 🚀 Features

* ✅ Simple `Agent` and `State` abstractions for building agent workflows.
* ⚙️ Support for parallel state transitions, condition checks, and results.
* 🔍 Plug-and-play: easily inject your own function handlers.
* 📦 .NET Standard compatible – works across .NET Framework and .NET Core.
* ✅ StateMachine Code is completely decoupled from Agent pipelines
* 🧠 `AgentStateMachine` for creating stateful multi-agent workflows
* 🌐 `LombdaAgent` to unify Agent and StateMachine operations
* 🪟 Debugging UI for Windows with streaming chat and verbose logging
* 📢 Event system for monitoring agent operations and debugging
* 📦 BabyAGI
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
LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41Mini,
                        [new ProviderAuthentication(LLmProviders.OpenAi, 
                        Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

Agent agent = new Agent(client, "Assistant", "Have fun");

RunResult result = await Runner.RunAsync(agent, "Hello World!");
```
### Automatic Structured Output from Type
```csharp
[Description("Steps to complete problem")]
public struct math_step
{
    public string explanation { get; set; }
    public string output { get; set; }
}

Agent agent = new Agent(client,  _output_schema:typeof(math_step));

RunResult result = await Runner.RunAsync(agent, "How can I solve 8x + 7 = -23?");

//Helper function to extract json from last message
math_step mathResult = result.ParseJson<math_step>();
```
### Simple Tool Use
```csharp
void async Task Run()
{
    Agent agent = new Agent(
        client
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
            client, 
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

ResearchState.AddTransition(reportingState); //Use Lambda expression For passthrough to reporting state

reportingState.AddTransition(new ExitState()); //Use Lambda expression For passthrough to Exit### Run the StateMachine//Create State Machine Runner
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

### Creating State Machines With Input & Output Types
```csharp
//Where string is Input and int is Output
StateMachine<string, int> stateMachine = new();

//Set start state with string Input
stateMachine.SetEntryState(inputState);

//Set state where output is
stateMachine.SetOutputState(resultState);

//Return list of Output objects from State 
//List because machine might generate more than 1 output depending on flow
List<int> stateResults = await stateMachine.Run("3");---
### Allow states to transition to other states in a parallel workflow 
//AllowsParallelTransitions = true Allows state Outputs to transition to all states that meet the criteria
ConvertStringToIntState inputState = new() { AllowsParallelTransitions = true };

IntPlus3State state3 = new();
IntPlus4State state4 = new();

//CombineInput = true only does 1 execution reguardless of # of Inputs
//Handle all of the Inputs
SummingState summingState = new() { CombineInput = true };
ConvertIntToStringState resultState = new();

//should happen in parallel and get result
inputState.AddTransition(state3);
inputState.AddTransition(state4);

//summing State will get both results next tick
state3.AddTransition(summingState);
state4.AddTransition(summingState);

//Will sum all inputs 
summingState.AddTransition(resultState);

//Convert result and End the State Machine 
resultState.AddTransition(new ExitState());

//Create Input & Output State Machine
StateMachine<string, string> stateMachine = new();

//Define Entry and Output States
stateMachine.SetEntryState(inputState);
stateMachine.SetOutputState(resultState);

//Run the StateMachine
List<string?> stateResults = await stateMachine.Run("3");
```

### Simple Conversion from different Input to Output types

```csharp
ConvertStringToIntState inputState = new();
ConvertStringToIntState resultState = new();

//Input State will convert string to int
inputState.AddTransition((result) => result.ToString(), resultState);

resultState.AddTransition(new ExitState());

StateMachine<string, int?> stateMachine = new();

stateMachine.SetEntryState(inputState);
stateMachine.SetOutputState(resultState);

var stateResults = await stateMachine.Run(["3","2","4"]);

Console.WriteLine($"State Results: {string.Join(", ", stateResults.Select(r => string.Join(", ", r)))}");

Assert.IsTrue(stateResults[0].Contains(3));
```
## New Features

### AgentState and AgentStateMachine

The `AgentState` system enhances the basic state machine by creating states specifically designed for agent execution. Unlike the `BaseState`, which is a generic state container, `AgentState` is tailored for AI agent operations.

```csharp
class ReportingState : AgentState<string, ReportData>
{
    public ReportingState(StateMachine stateMachine) : base(stateMachine){}

    public override Agent InitilizeStateAgent()
    {
        return new Agent(
            client: new OpenAIModelClient("gpt-4o-mini"), 
            _name: "Reporting agent",
            _instructions: """
                You are a senior researcher tasked with writing a cohesive report for a research query.
                You will be provided with the original query, and some initial research done by a research assistant.
                Generate a detailed report in markdown format.
                """, 
            _output_schema: typeof(ReportData)
        );
    }

    public override async Task<ReportData> Invoke(string input)
    {
        return await BeginRunnerAsync<ReportData>(input);
    }
}
```

### Using the LombdaAgent for Orchestration

The `LombdaAgent` acts as a unifier between individual agents and the state machine. It provides centralized management, event handling, and debugging capabilities.

```csharp
// Create a LombdaAgent instance
LombdaAgent lombdaAgent = new LombdaAgent();

// Create and run a stateful agent workflow
ResearchAgent researchAgent = new ResearchAgent(lombdaAgent);
ReportData result = await researchAgent.RunAsync("Research quantum computing applications in medicine");

// Monitor events and get logs
lombdaAgent.VerboseCallback += (message) => Console.WriteLine($"[VERBOSE] {message}");
lombdaAgent.StreamingCallback += (update) => Console.WriteLine($"[STREAM] {update}");
```
### Creating Multi-Agent Workflows with AgentStateMachine

The `AgentStateMachine` provides a framework for creating complex multi-agent workflows:

```csharp
public class ResearchAgent : AgentStateMachine<string, ReportData>
{
    public ResearchAgent(LombdaAgent lombdaAgent) : base(lombdaAgent) { }

    public override void InitilizeStates()
    {
        // Setup states
        PlanningState plannerState = new PlanningState(this);
        ResearchState researchState = new ResearchState(this);
        ReportingState reportingState = new ReportingState(this);

        // Setup transitions between states
        plannerState.AddTransition((result) => result.items.Length > 0, researchState);
        researchState.AddTransition(reportingState);
        reportingState.AddTransition(new ExitState());

        // Set entry and output states
        SetEntryState(plannerState);
        SetOutputState(reportingState);
    }
}

// Use the state machine
var agent = new ResearchAgent(new LombdaAgent());
var result = await agent.RunAsync("Research quantum computing");
```
### Event System for Logging and Debugging

The new event system provides comprehensive monitoring and debugging capabilities:
```csharp
// Subscribe to verbose logs
lombdaAgent.RunningVerboseCallback += (message) => {
    Debug.WriteLine($"[VERBOSE]: {message}");
};

// Subscribe to streaming updates
lombdaAgent.RunningStreamingCallback += (update) => {
    UI.UpdateStreamingPanel(update);
};
```

### Debugging UI for Windows

LombdaAgentSDK now includes a basic debugging UI for Windows applications:

- Left panel: Streaming chat updates in real-time
- Right panel: Verbose logging with detailed operation information
- Allows monitoring the state transitions and agent communications

To use the debugging UI, reference the WinFormsAgentUI project in your solution.

## 📚 Documentation

For comprehensive documentation, please visit the [docs folder](docs/):

- **[Getting Started Guide](docs/GETTING_STARTED.md)** - Step-by-step tutorial for new users
- **[API Reference](docs/API_REFERENCE.md)** - Complete API documentation  
- **[Architecture Guide](docs/ARCHITECTURE.md)** - Design principles and architecture overview
- **[Examples Documentation](docs/EXAMPLES.md)** - Detailed examples and use cases
- **[Contributing Guide](docs/CONTRIBUTING.md)** - Guidelines for contributors

## 🚦 Roadmap
* [ ] Add non async support for agent execution.
* [ ] Improve logging and diagnostics.
* [ ] Enhance the debugging UI for better visualization of agent states
* [ ] Add support for serializing agent state machines for persistence
* [ ] Fix structured outputs deserialization with field descriptions

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you'd like to change.

---

## 📄 License

[MIT](LICENSE)

---

## 🙌 Acknowledgements

[LlmTornado](https://github.com/lofcz/LlmTornado)
[openai-dotnet](https://github.com/openai/openai-dotnet)


