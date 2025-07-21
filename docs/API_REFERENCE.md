# API Reference

This document provides a comprehensive reference for the LombdaAgentSDK API.

## Core Classes

### Agent

The primary class for creating AI agents.

```csharp
public Agent(
    IModelClient client,
    string _name,
    string _instructions,
    Tool[]? _tools = null,
    Type? _output_schema = null,
    string? _output_schema_json = null
)
```
#### Parameters

- `client`: Model provider client
- `_name`: Name for the agent (used in conversation)
- `_instructions`: System prompt/instructions
- `_tools`: Array of tools available to the agent
- `_output_schema`: Type for structured output
- `_output_schema_json`: JSON schema for structured output

#### Example

```csharp
Agent plannerAgent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Planner",
    "Create a research plan for the given query",
    typeof(ResearchPlan)
);

var result = await Runner.RunAsync(plannerAgent, query);
return result.ParseJson<ResearchPlan>();
```
### LombdaAgent

The orchestrator class that unifies Agent and StateMachine operations, providing event management, debugging, and monitoring capabilities.

```csharp
public class LombdaAgent
{
    public LombdaAgent();
    public void AddStateMachine(IAgentStateMachine stateMachine);
    public void RemoveStateMachine(IAgentStateMachine stateMachine);
}
```
#### Events

- `VerboseCallback`: Triggered for verbose log messages
- `StreamingCallback`: Triggered for streaming updates
- `RunningVerboseCallback`: Event for receiving verbose log messages
- `RunningStreamingCallback`: Event for receiving streaming updates

#### Example

```csharp
// Create a LombdaAgent
LombdaAgent agent = new LombdaAgent();

// Subscribe to events
agent.RunningVerboseCallback += (message) => Console.WriteLine($"[LOG] {message}");
agent.RunningStreamingCallback += (update) => Console.WriteLine($"[STREAM] {update}");

// Use with a state machine
ResearchAgent researchAgent = new ResearchAgent(agent);
var result = await researchAgent.RunAsync("Research quantum computing");
```
### BaseState

Base class for creating state machine states.

```csharp
public abstract class BaseState<TInput, TOutput>
{
    public virtual async Task<TOutput> Invoke(TInput input);
    public void AddTransition(TransitionEvent<TOutput> transitionCheck, BaseState nextState);
    public void AddTransition(BaseState nextState);
    public void AddTransition<TNextInput>(TransitionEvent<TOutput> transitionCheck, 
                                         Func<TOutput, TNextInput> converter, BaseState nextState);
}
```
#### Properties

- `StateMachine? CurrentStateMachine` - Reference to containing state machine
- `TInput Input` - Input data for the state
- `TOutput? Output` - Output data from state execution
- `bool IsDeadEnd` - Indicates if state is a dead end (no valid transitions)
- `bool AllowsParallelTransitions` - Enable multiple transition paths
- `bool CombineInput` - Combine multiple inputs into single execution

#### Example

```csharp
class PlanningState : BaseState<string, ResearchPlan>
{
    public override async Task<ResearchPlan> Invoke(string input)
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
### AgentState

Specialized state for agent execution with built-in event handling and agent lifecycle management.

```csharp
public abstract class AgentState<TInput, TOutput> : BaseState<TInput, TOutput>, IAgentState
{
    public AgentState(StateMachine stateMachine);
    public abstract Agent InitilizeStateAgent();
    protected async Task<TResult> BeginRunnerAsync<TResult>(string input);
}
```
#### Properties and Events

- `Agent StateAgent`: The agent controlled by this state
- `RunnerVerboseCallbacks? RunnerVerboseCallbacks`: Callbacks for verbose logs
- `StreamingCallbacks? StreamingCallbacks`: Callbacks for streaming updates
- `event Action<string>? RunningVerboseCallback`: Event for verbose messages
- `event Action<string>? RunningStreamingCallback`: Event for streaming updates
- `CancellationTokenSource CancelTokenSource`: Cancellation token source

#### Example

```csharp
class ResearchState : AgentState<string, ResearchData>
{
    public ResearchState(StateMachine stateMachine) : base(stateMachine) {}

    public override Agent InitilizeStateAgent()
    {
        return new Agent(
            client: new OpenAIModelClient("gpt-4o-mini"),
            _name: "Research Agent",
            _instructions: "You are a researcher tasked with finding information.",
            _output_schema: typeof(ResearchData)
        );
    }

    public override async Task<ResearchData> Invoke(string input)
    {
        return await BeginRunnerAsync<ResearchData>(input);
    }
}
```
### AgentStateMachine

State machine specifically designed for agent-based workflows.

```csharp
public abstract class AgentStateMachine<TInput, TOutput> : StateMachine<TInput, TOutput>, IAgentStateMachine
{
    public AgentStateMachine(LombdaAgent lombdaAgent);
    public abstract void InitilizeStates();
    public async Task<TOutput> RunAsync(TInput input);
}
```
#### Properties

- `LombdaAgent ControlAgent`: The controller agent for this state machine
- `List<ModelItem> SharedModelItems`: Shared conversation history

#### Example

```csharp
public class ResearchAgent : AgentStateMachine<string, ReportData>
{
    public ResearchAgent(LombdaAgent lombdaAgent) : base(lombdaAgent) {}

    public override void InitilizeStates()
    {
        // Setup states
        PlanningState plannerState = new PlanningState(this);
        ResearchState researchState = new ResearchState(this);
        ReportingState reportingState = new ReportingState(this);

        // Setup transitions
        plannerState.AddTransition((plan) => plan.items.Length > 0, researchState);
        researchState.AddTransition(reportingState);
        reportingState.AddTransition(new ExitState());

        // Set entry and output states
        SetEntryState(plannerState);
        SetOutputState(reportingState);
    }
}

// Usage
LombdaAgent agent = new LombdaAgent();
ResearchAgent researchAgent = new ResearchAgent(agent);
ReportData report = await researchAgent.RunAsync("Research quantum computing");
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
- `List<IAgentState> States` - Collection of agent states

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
## Event System

### Event Delegates

```csharp
// Callbacks for runner operations
public delegate void RunnerVerboseCallbacks(string runnerAction);

// Callbacks for streaming results
public delegate void StreamingCallbacks(string streamingResult);

// Events for state transitions
public event Action? OnBegin;
public event Action<StateTransition>? OnStateEntered;
public event Action<IState>? OnStateExited;
public event Action? FinishedTriggered;
public event Action? CancellationTriggered;
```
## UI Debugging

The Windows debugging UI provides visualization of agent operations:

```csharp
// Initialize the debug UI
var debugUI = new AgentDebuggerForm();

// Connect LombdaAgent to UI
lombdaAgent.RunningVerboseCallback += debugUI.AddVerboseLog;
lombdaAgent.RunningStreamingCallback += debugUI.AddStreamingMessage;

// Show the UI
debugUI.Show();
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