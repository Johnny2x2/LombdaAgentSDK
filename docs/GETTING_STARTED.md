# Getting Started

This guide will help you quickly get up and running with LombdaAgentSDK.

## Installation

Install the SDK using NuGet:
```bash
dotnet add package LombdaAiAgents
```

Or clone the repository and reference the project:
```bash
git clone https://github.com/johnny2x2/LombdaAgentSDK.git
```
## Basic Usage

### Creating a Simple Agent

```csharp
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;

// Create a simple agent
Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Assistant",
    "You are a helpful assistant."
);

// Run the agent
RunResult result = await Runner.RunAsync(agent, "Tell me a joke");
Console.WriteLine(result.Text);
```
### Using Structured Output

```csharp
// Define a structured output type
public class WeatherReport
{
    public string Location { get; set; }
    public double Temperature { get; set; }
    public string Conditions { get; set; }
    public string[] ForecastDays { get; set; }
}

// Create an agent with structured output
Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Weather Reporter",
    "You are a weather reporting assistant.",
    _output_schema: typeof(WeatherReport)
);

// Run the agent
RunResult result = await Runner.RunAsync(agent, "What's the weather in New York?");
WeatherReport report = result.ParseJson<WeatherReport>();

Console.WriteLine($"Temperature: {report.Temperature}°C");
Console.WriteLine($"Conditions: {report.Conditions}");
```
### Adding Tools

```csharp
// Define a tool
[Tool(
    Description = "Get current weather in a location",
    In_parameters_description = [
        "The city and state, e.g. San Francisco, CA",
        "The temperature unit to use: 'celsius' or 'fahrenheit'"
    ]
)]
public string GetWeather(string location, string unit = "celsius")
{
    // In a real app, you would call a weather API here
    return $"72°F, partly cloudy in {location}";
}

// Create an agent with tools
Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Weather Assistant",
    "You are a weather assistant.",
    _tools: [GetWeather] // Pass method group as tool
);

// Run the agent
RunResult result = await Runner.RunAsync(agent, "What's the weather in Boston?");
Console.WriteLine(result.Text);
```
## State Machine Basics

### Creating a Simple State

```csharp
using LombdaAgentSDK.StateMachine;

// Define a state that processes string input and produces integer output
public class StringToIntState : BaseState<string, int>
{
    public override async Task<int> Invoke(string input)
    {
        return int.Parse(input);
    }
}

// Define another state that formats integers
public class IntToResultState : BaseState<int, string>
{
    public override async Task<string> Invoke(int input)
    {
        return $"The result is {input * 2}";
    }
}
```
### Connecting States

```csharp
// Create state instances
StringToIntState inputState = new();
IntToResultState resultState = new();

// Connect states: When inputState completes, transition to resultState
inputState.AddTransition(_ => true, resultState);

// Mark the end of the workflow
resultState.AddTransition(_ => true, new ExitState());

// Create and configure state machine
StateMachine<string, string> stateMachine = new();
stateMachine.SetEntryState(inputState);
stateMachine.SetOutputState(resultState);

// Execute the workflow
List<string?> results = await stateMachine.Run("42");
Console.WriteLine(results.First()); // "The result is 84"
```
## Agent State Machine

### Creating Agent States

```csharp
using LombdaAgentSDK.AgentStateSystem;

// Define a research plan structure
public class ResearchPlan
{
    public string[] items { get; set; }
}

// Define an agent state for planning
class PlanningState : AgentState<string, ResearchPlan>
{
    public PlanningState(StateMachine stateMachine) : base(stateMachine) { }

    public override Agent InitilizeStateAgent()
    {
        return new Agent(
            new OpenAIModelClient("gpt-4o-mini"),
            "Planner",
            "Create a research plan for the given query.",
            _output_schema: typeof(ResearchPlan)
        );
    }

    public override async Task<ResearchPlan> Invoke(string input)
    {
        return await BeginRunnerAsync<ResearchPlan>(input);
    }
}

// Define an agent state for generating reports
class ReportState : AgentState<ResearchPlan, string>
{
    public ReportState(StateMachine stateMachine) : base(stateMachine) { }

    public override Agent InitilizeStateAgent()
    {
        return new Agent(
            new OpenAIModelClient("gpt-4o-mini"),
            "Report Generator",
            "Generate a comprehensive report based on the research plan.",
            _output_schema: typeof(string)
        );
    }

    public override async Task<string> Invoke(ResearchPlan plan)
    {
        return await BeginRunnerAsync<string>($"Research plan: {string.Join(", ", plan.items)}");
    }
}
```
### Creating an Agent State Machine

```csharp
using LombdaAgentSDK.AgentStateSystem;

public class ResearchAgent : AgentStateMachine<string, string>
{
    public ResearchAgent(LombdaAgent lombdaAgent) : base(lombdaAgent) { }

    public override void InitilizeStates()
    {
        // Create states
        PlanningState planningState = new PlanningState(this);
        ReportState reportState = new ReportState(this);

        // Setup transitions
        planningState.AddTransition((plan) => plan.items.Length > 0, reportState);
        reportState.AddTransition(new ExitState());

        // Set entry and output states
        SetEntryState(planningState);
        SetOutputState(reportState);
    }
}

// Use the agent state machine
LombdaAgent lombdaAgent = new LombdaAgent();
ResearchAgent researchAgent = new ResearchAgent(lombdaAgent);
string report = await researchAgent.RunAsync("Research the best electric bikes under $1500");
Console.WriteLine(report);
```
### Using the LombdaAgent for Monitoring and Debugging

```csharp
// Create a LombdaAgent with event handling
LombdaAgent lombdaAgent = new LombdaAgent();

// Subscribe to verbose logs
lombdaAgent.RunningVerboseCallback += (message) => {
    Console.WriteLine($"[LOG] {message}");
};

// Subscribe to streaming updates
lombdaAgent.RunningStreamingCallback += (update) => {
    Console.WriteLine($"[STREAM] {update}");
};

// Create and run an agent state machine
ResearchAgent researchAgent = new ResearchAgent(lombdaAgent);
string report = await researchAgent.RunAsync("Research quantum computing applications");

// Output will include all the verbose logs and streaming updates
```
### Using the Windows Debugging UI

If you're developing on Windows, you can use the included debugging UI:

```csharp
using WinFormsAgentUI;

// Create the debug form
var debugForm = new AgentDebuggerForm();

// Connect the LombdaAgent to the UI
lombdaAgent.RunningVerboseCallback += debugForm.AddVerboseLog;
lombdaAgent.RunningStreamingCallback += debugForm.AddStreamingMessage;

// Show the form
debugForm.Show();

// Run your agent workflows
ResearchAgent researchAgent = new ResearchAgent(lombdaAgent);
await researchAgent.RunAsync("Research topic");

// The UI will display logs and streaming chat in real-time
```
## Using LLMTornado Provider

LLMTornado provides access to multiple model providers:

```csharp
using LlmTornado.Models;
using LlmTornado.Providers;

LLMTornadoModelProvider client = new(
    ChatModel.OpenAi.Gpt41.V41Mini,
    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)]
);

Agent agent = new Agent(client, "Assistant", "You are helpful");
```
## Advanced Features

### Streaming Responses

```csharp
RunResult result = await Runner.RunAsync(
    agent, 
    "Tell me a story",
    streaming: true,
    streamingCallback: (text) => Console.Write(text)
);
```
### Guard Rails

```csharp
public static async Task<GuardRailFunctionOutput> ContentFilter(string input)
{
    // Check for inappropriate content
    bool isInappropriate = input.Contains("badword");
    
    return new GuardRailFunctionOutput
    {
        TripwireTriggered = isInappropriate,
        OutputInfo = isInappropriate ? "Content blocked" : "Content approved"
    };
}

RunResult result = await Runner.RunAsync(
    agent, 
    "User input here",
    guard_rail: ContentFilter
);
```
### Parallel State Processing

```csharp
public class ParallelProcessingState : BaseState<string, ProcessedData>
{
    public ParallelProcessingState()
    {
        AllowsParallelTransitions = true; // Enable parallel transitions
    }
    
    public override async Task<ProcessedData> Invoke(string input)
    {
        // Process input
        return new ProcessedData { Result = $"Processed: {input}" };
    }
}
```
### Computer Use (Experimental)

```csharp
RunResult result = await Runner.RunAsync(
    agent, 
    "Take a screenshot",
    computerUseCallback: (action) => {
        Console.WriteLine($"Computer action: {action.TypeText}");
    }
);
```
### Sharing Data Between States

You can share data between states in an AgentStateMachine using SharedModelItems:

```csharp
// In your state's Invoke method
var newModelItem = new ModelItem
{
    Role = ModelItemRoles.Assistant,
    Content = "Important information to share"
};

// Add to shared items
CurrentStateMachine.RuntimeProperties["SharedKey"] = "Shared value";

// Access in another state
if (CurrentStateMachine.RuntimeProperties.TryGetValue("SharedKey", out object value))
{
    string sharedValue = value.ToString();
    // Use the shared value
}
```
## Best Practices

### 1. Error Handling

```csharp
try
{
    RunResult result = await Runner.RunAsync(agent, "User input");
    Console.WriteLine(result.Text);
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Input blocked: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```
### 2. Resource Management

```csharp
// Set appropriate limits for state machines
StateMachine stateMachine = new()
{
    MaxThreads = 5 // Limit concurrent states
};

```
### 3. Type Safety

Always use strongly typed states and structured output for better reliability:

```csharp
// Good: Strongly typed
public class TypedState : BaseState<UserQuery, ProcessedResult>

// Avoid: Generic object types
public class GenericState : BaseState<object, object>
```
## Next Steps

- Explore the [API Reference](API_REFERENCE.md) for detailed class documentation
- Read the [Architecture Guide](ARCHITECTURE.md) to understand the design
- Check out the [Examples](../Examples/) folder for more complex scenarios
- Review the [Contributing Guide](CONTRIBUTING.md) if you want to contribute

## Common Issues

### API Key Not Set

**Error:** `Value cannot be null. (Parameter 'key')`

**Solution:** Set your `OPENAI_API_KEY` environment variable.

### State Transition Failures

**Issue:** State transitions fail silently

**Solution:** Check that your transition conditions return `true` and that input/output types match between connected states.

### Tool Not Found

**Issue:** Tool method not being called

**Solution:** Ensure methods are public, have the `[Tool]` attribute, and are included in the agent's tools list.
