# Getting Started with LombdaAgentSDK

This guide will help you get up and running with LombdaAgentSDK, from basic agent creation to advanced state machine workflows.

## Prerequisites

- .NET 8.0 or later
- An OpenAI API key (for OpenAI provider) or LLMTornado setup
- Visual Studio, VS Code, or any C# development environment

## Installation

### NuGet Package

```bash
dotnet add package LombdaAiAgents
```

### From Source

1. Clone the repository:
```bash
git clone https://github.com/Johnny2x2/LombdaAgentSDK.git
```

2. Add project reference:
```bash
dotnet add reference path/to/LombdaAgentSDK/LombdaAgentSDK.csproj
```

## Quick Start: Your First Agent

### Step 1: Create a Simple Agent

```csharp
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;

// Create an agent with OpenAI
Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"), 
    "Assistant", 
    "You are a helpful assistant"
);

// Run the agent
RunResult result = await Runner.RunAsync(agent, "Hello, world!");
Console.WriteLine($"Agent: {result.Text}");
```

### Step 2: Set Your API Key

Make sure to set your OpenAI API key as an environment variable:

```bash
export OPENAI_API_KEY="your-api-key-here"
```

Or in your code:
```csharp
Environment.SetEnvironmentVariable("OPENAI_API_KEY", "your-api-key-here");
```

## Adding Tools to Your Agent

Tools allow agents to perform actions and access external data.

### Step 1: Create Tool Methods

```csharp
using LombdaAgentSDK.Agents.Tools;

public class WeatherTools
{
    [Tool(Description = "Get current weather for a location")]
    public string GetWeather(string location)
    {
        // Simulate weather API call
        return $"The weather in {location} is 72°F and sunny";
    }

    [Tool(Description = "Get weather forecast for multiple days")]
    public string GetForecast(string location, int days = 3)
    {
        return $"{days}-day forecast for {location}: Sunny with highs in the 70s";
    }
}
```

### Step 2: Add Tools to Agent

```csharp
var weatherTools = new WeatherTools();

Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Weather Assistant",
    "You help users with weather information",
    _tools: [weatherTools.GetWeather, weatherTools.GetForecast]
);

RunResult result = await Runner.RunAsync(agent, "What's the weather like in Boston?");
Console.WriteLine(result.Text);
```

## Structured Output

Get structured data from your agents using C# types.

### Step 1: Define Your Data Structure

```csharp
public struct WeatherReport
{
    public string location { get; set; }
    public int temperature { get; set; }
    public string conditions { get; set; }
    public string recommendation { get; set; }
}
```

### Step 2: Use Structured Output

```csharp
Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Weather Analyzer", 
    "Analyze weather and provide structured recommendations",
    _output_schema: typeof(WeatherReport)
);

RunResult result = await Runner.RunAsync(agent, "Analyze the weather in Miami");

// Parse the structured output
WeatherReport report = result.ParseJson<WeatherReport>();
Console.WriteLine($"Temperature: {report.temperature}°F");
Console.WriteLine($"Conditions: {report.conditions}");
```

## Building Your First State Machine

State machines allow you to create complex workflows with multiple steps.

### Step 1: Create State Classes

```csharp
using LombdaAgentSDK.StateMachine;

// Planning state: Takes user query, outputs research plan
public class PlanningState : BaseState<string, ResearchPlan>
{
    public override async Task<ResearchPlan> Invoke(string query)
    {
        Agent planner = new Agent(
            new OpenAIModelClient("gpt-4o-mini"),
            "Research Planner",
            "Create a detailed research plan with search terms and steps",
            typeof(ResearchPlan)
        );
        
        var result = await Runner.RunAsync(planner, query);
        return result.ParseJson<ResearchPlan>();
    }
}

// Research state: Takes plan, outputs research data
public class ResearchState : BaseState<ResearchPlan, ResearchData>
{
    public override async Task<ResearchData> Invoke(ResearchPlan plan)
    {
        // Implement research logic
        return new ResearchData { Summary = "Research completed", Sources = plan.SearchTerms };
    }
}

// Report state: Takes data, outputs final report
public class ReportState : BaseState<ResearchData, string>
{
    public override async Task<string> Invoke(ResearchData data)
    {
        Agent reporter = new Agent(
            new OpenAIModelClient("gpt-4o-mini"),
            "Report Writer",
            "Write a comprehensive report based on research data"
        );
        
        var result = await Runner.RunAsync(reporter, $"Write a report based on: {data.Summary}");
        return result.Text;
    }
}
```

### Step 2: Define Data Structures

```csharp
public struct ResearchPlan
{
    public List<string> SearchTerms { get; set; }
    public List<string> Steps { get; set; }
}

public struct ResearchData
{
    public string Summary { get; set; }
    public List<string> Sources { get; set; }
}
```

### Step 3: Connect States with Transitions

```csharp
// Create state instances
var planningState = new PlanningState();
var researchState = new ResearchState();
var reportState = new ReportState();

// Setup transitions between states
planningState.AddTransition(plan => plan.SearchTerms?.Count > 0, researchState);
researchState.AddTransition(_ => true, reportState);
reportState.AddTransition(_ => true, new ExitState());
```

### Step 4: Run the State Machine

```csharp
// Create and configure state machine
StateMachine<string, string> stateMachine = new();
stateMachine.SetEntryState(planningState);
stateMachine.SetOutputState(reportState);

// Execute the workflow
List<string?> results = await stateMachine.Run("Research the best electric bikes under $1500");

// Get the final report
string finalReport = results.First();
Console.WriteLine($"Final Report: {finalReport}");
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
```
Error: Value cannot be null. (Parameter 'key')
```
Solution: Set your `OPENAI_API_KEY` environment variable.

### State Transition Failures
```
State transitions fail silently
```
Solution: Check that your transition conditions return `true` and that input/output types match between connected states.

### Tool Not Found
```
Tool method not being called
```
Solution: Ensure methods are public, have the `[Tool]` attribute, and are included in the agent's tools list.