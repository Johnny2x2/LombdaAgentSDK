# LombdaAgentSDK API Reference

## Core Classes

### Agent Class

The `Agent` class is the core component for creating AI agents with tool capabilities and instructions.

#### Constructor

```csharp
public Agent(ModelClient client, string name, string instructions = "", Type? outputSchema = null, List<Delegate>? tools = null)
```

**Parameters:**
- `client`: Model provider client (OpenAI, LlmTornado, etc.)
- `name`: Agent name identifier
- `instructions`: System instructions for the agent behavior
- `outputSchema`: Optional type for structured JSON output
- `tools`: List of available tools/functions

#### Properties

- `ModelClient Client` - The model provider client
- `string AgentName` - Name of the agent
- `string Instructions` - Instructions for processing prompts
- `Type? OutputSchema` - Data type for formatted response output
- `List<Delegate>? Tools` - Available tools for the agent
- `ModelResponseOptions Options` - Response configuration options

#### Example

```csharp
Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"), 
    "Assistant", 
    "You are a helpful research assistant",
    typeof(ResearchResult),
    [GetWeatherTool, SearchWebTool]
);
```

### Runner Class

Static class for executing agent workflows with various configuration options.

#### RunAsync Method

```csharp
public static async Task<RunResult> RunAsync(
    Agent agent,
    string input = "",
    GuardRailFunction? guardRail = null,
    bool singleTurn = false,
    int maxTurns = 10,
    List<ModelItem>? messages = null,
    ComputerActionCallbacks? computerUseCallback = null,
    RunnerVerboseCallbacks? verboseCallback = null,
    bool streaming = false,
    StreamingCallbacks? streamingCallback = null,
    string responseID = ""
)
```

**Parameters:**
- `agent`: The agent to execute
- `input`: User input message
- `guardRail`: Optional input validation function
- `singleTurn`: Set to true for single interaction
- `maxTurns`: Maximum conversation turns (default: 10)
- `messages`: Previous conversation history
- `computerUseCallback`: Callback for computer use actions
- `verboseCallback`: Callback for verbose logging
- `streaming`: Enable streaming responses
- `streamingCallback`: Callback for streaming content
- `responseID`: Previous response ID for continuation

#### Example

```csharp
RunResult result = await Runner.RunAsync(
    agent, 
    "What is the weather in Boston?",
    maxTurns: 5,
    streaming: true,
    streamingCallback: Console.Write
);
```

### BaseState Class

Abstract base class for creating state machine states with typed input/output.

#### BaseState<TInput, TOutput>

Generic state class with strongly typed input and output.

```csharp
public abstract class BaseState<TInput, TOutput> : BaseState
```

#### Key Methods

```csharp
public abstract Task<TOutput> Invoke(TInput input);
public virtual Task EnterState(TInput? input);
public virtual Task ExitState();
public void AddTransition(TransitionEvent<TOutput> condition, BaseState nextState);
```

#### Properties

- `List<TInput> Input` - Input data for the state
- `List<TOutput> Output` - Output results from the state
- `bool AllowsParallelTransitions` - Enable parallel state transitions
- `bool CombineInput` - Process all inputs as single batch
- `StateMachine? CurrentStateMachine` - Reference to parent state machine

#### Example

```csharp
class PlanningState : BaseState<string, ResearchPlan>
{
    public override async Task<ResearchPlan> Invoke(string query)
    {
        Agent plannerAgent = new Agent(
            new OpenAIModelClient("gpt-4o-mini"),
            "Planner",
            "Create a research plan for the given query",
            typeof(ResearchPlan)
        );
        
        var result = await Runner.RunAsync(plannerAgent, query);
        return result.ParseJson<ResearchPlan>();
    }
}
```

### StateMachine Class

Orchestrates execution of connected states with transition logic.

#### Generic StateMachine<TInput, TOutput>

```csharp
public class StateMachine<TInput, TOutput> : StateMachine
```

#### Key Methods

```csharp
public async Task<List<TOutput?>> Run(TInput input);
public async Task<List<List<TOutput?>>> Run(TInput[] inputs);
public void SetEntryState(BaseState startState);
public void SetOutputState(BaseState resultState);
```

#### Properties

- `List<TOutput>? Results` - Final results from execution
- `int MaxThreads` - Maximum concurrent threads (default: 20)
- `CancellationToken CancelToken` - Cancellation token for stopping execution
- `Dictionary<string, object> RuntimeProperties` - Runtime state storage

#### Example

```csharp
// Create states
PlanningState planningState = new PlanningState();
ResearchState researchState = new ResearchState();
ReportState reportState = new ReportState();

// Setup transitions
planningState.AddTransition(plan => plan != null, researchState);
researchState.AddTransition(_ => true, reportState);
reportState.AddTransition(_ => true, new ExitState());

// Create and run state machine
StateMachine<string, string> stateMachine = new();
stateMachine.SetEntryState(planningState);
stateMachine.SetOutputState(reportState);

List<string?> results = await stateMachine.Run("Research electric bikes under $1500");
```

### RunResult Class

Contains the results and conversation history from agent execution.

#### Properties

- `string Text` - Final text response from agent
- `List<ModelItem> Messages` - Complete conversation history
- `ModelResponse Response` - Raw model response data

#### Methods

```csharp
public T ParseJson<T>() // Parse structured output to specified type
```

### Tool Attribute

Attribute for marking methods as agent tools with metadata.

```csharp
[Tool(
    Description = "Get current weather for a location",
    In_parameters_description = [
        "City and state, e.g. Boston, MA",
        "Temperature unit (celsius/fahrenheit)"
    ]
)]
public string GetWeather(string location, Unit unit = Unit.celsius)
{
    // Tool implementation
    return "75°F, sunny";
}
```

## Model Providers

### OpenAIModelClient

Client for OpenAI models with built-in tool calling and structured output support.

```csharp
public OpenAIModelClient(string model, ApiKeyCredential? apiKey = null)
```

### LLMTornadoModelProvider

Client for LLMTornado with support for multiple model providers.

```csharp
LLMTornadoModelProvider client = new(
    ChatModel.OpenAi.Gpt41.V41Mini,
    [new ProviderAuthentication(LLmProviders.OpenAi, apiKey)]
);
```

## Utility Classes

### json_util

Helper methods for JSON parsing and schema generation.

```csharp
public static T ParseJson<T>(RunResult result);
public static ModelOutputFormat CreateJsonSchemaFormatFromType(this Type type, bool strict = false);
```

### ImageUtility

Helper methods for image processing and encoding.

### ComputerToolUtility

Utilities for computer use functionality including screenshot capture and UI automation.

## Exception Classes

### GuardRailTriggerException

Thrown when input guard rails are triggered to prevent agent execution.

## Delegates and Callbacks

```csharp
public delegate void ComputerActionCallbacks(ComputerToolAction computerCall);
public delegate void RunnerVerboseCallbacks(string runnerAction);
public delegate void StreamingCallbacks(string streamingResult);
public delegate Task<GuardRailFunctionOutput> GuardRailFunction(string input = "");
public delegate bool TransitionEvent<T>(T output);
```