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
List<int> stateResults = await stateMachine.Run("3");
```
---
### Allow states to transition to other states in a parallel workflow 

```csharp
//AllowsParallelTransitions = true Allows state Outputs to transition to all states that meet the criteria
ConvertStringToIntState inputState = new() { AllowsParallelTransitions = true };

IntPlus3State state3 = new();
IntPlus4State state4 = new();

//CombineInput = true only does 1 execution reguardless of # of Inputs
//Handle all of the Inputs
SummingState summingState = new() { CombineInput = true };
ConvertIntToStringState resultState = new();

//should happen in parallel and get result
inputState.AddTransition(_=> true, state3);
inputState.AddTransition(_ => true, state4);

//summing State will get both results next tick
state3.AddTransition(_ => true, summingState);
state4.AddTransition(_ => true, summingState);

//Will sum all inputs 
summingState.AddTransition(_ => true, resultState);

//Convert result and End the State Machine 
resultState.AddTransition(_ => true, new ExitState());

//Create Input & Output State Machine
StateMachine<string, string> stateMachine = new();

//Define Entry and Output States
stateMachine.SetEntryState(inputState);
stateMachine.SetOutputState(resultState);

//Run the StateMachine
List<string?> stateResults = await stateMachine.Run("3");
```
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

[LlmTornado](https://github.com/lofcz/LlmTornado)
[openai-dotnet](https://github.com/openai/openai-dotnet)


