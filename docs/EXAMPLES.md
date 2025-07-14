# Examples Documentation

This document provides an overview of the example projects included in the LombdaAgentSDK repository, demonstrating various use cases and capabilities.

## Basic Examples

Located in `Examples/Basic/`, these examples demonstrate fundamental SDK usage.

### BasicHelloWorld

**File:** `BasicHelloWorld.cs`

Simple "Hello World" example showing basic agent creation and execution.

```csharp
Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), "Assistant", "Have fun");
RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");
Console.WriteLine($"[ASSISTANT]: {result.Text}");
```

**Key Concepts:**
- Basic agent creation
- Simple text interaction
- Getting text responses

### BasicToolExample

**File:** `BasicToolExample.cs`

Demonstrates how to create and use tools with agents.

```csharp
[Tool(Description = "Get the current weather in a given location")]
public string GetCurrentWeather(string location, Unit unit = Unit.celsius)
{
    return $"31 C";
}

Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"), 
    "Assistant", 
    "Have fun",  
    _tools : [GetCurrentLocation, GetCurrentWeather]
);
```

**Key Concepts:**
- Creating tool methods with `[Tool]` attribute
- Tool parameter descriptions
- Enum parameters in tools
- Tool execution flow

### BasicStructuredOutputsExample

**File:** `BasicStructuredOutputsExample.cs`

Shows how to get structured JSON output from agents.

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
    _output_schema: typeof(math_step)
);

RunResult result = await Runner.RunAsync(agent, "How can I solve 8x + 7 = -23?");
math_step mathResult = result.ParseJson<math_step>();
```

**Key Concepts:**
- Defining data structures for output
- Using `typeof()` for schema generation
- Parsing structured responses with `ParseJson<T>()`

### BasicAgentAsToolExample

**File:** `BasicAgentAsToolExample.cs`

Demonstrates using one agent as a tool for another agent.

**Key Concepts:**
- Agent composition
- Nested agent execution
- Tool chaining

### BasicGuardRailExample

**File:** `BasicGuardRailExample.cs`

Shows how to implement input validation and safety measures.

**Key Concepts:**
- Input validation
- Guard rail functions
- Safety mechanisms

### BasicHelloWorldStreaming

**File:** `BasicHelloWorldStreaming.cs`

Demonstrates streaming responses from agents.

**Key Concepts:**
- Real-time response streaming
- Streaming callbacks
- Progressive output display

## OpenAI Examples

Located in `Examples/OpenAI/`, these examples use OpenAI-specific features.

### BasicOpenAIWebSearch

**File:** `BasicOpenAIWebSearch.cs`

Shows how to enable web search capabilities with OpenAI models.

**Key Concepts:**
- Web search integration
- OpenAI-specific features
- External data access

### ComputerUseExample

**File:** `ComputerUseExample.cs`

Demonstrates computer use functionality for UI automation.

**Key Concepts:**
- Computer vision and control
- Screenshot capture
- UI automation
- Desktop interaction

## LLMTornado Examples

Located in `Examples/LlmTornado/`, these examples show how to use multiple model providers.

### BasicGuardRailExample

Shows guard rail implementation with LLMTornado provider.

### BasicStructuredOutputsExample

Demonstrates structured output with LLMTornado models.

**Key Concepts:**
- Multi-provider support
- Provider configuration
- Authentication handling

## Demo Applications

Located in `Examples/Demos/`, these are full-featured example applications.

### Research Agent

**Files:** `Demos/ResearchAgent/`

A complete research workflow using state machines.

**Components:**
- `PlanningState.cs` - Creates research plans
- `ResearchState.cs` - Executes research
- `ReportState.cs` - Generates reports

**State Flow:**
```
User Query → Planning → Research → Report → Final Output
```

**Key Concepts:**
- Multi-state workflows
- State transitions
- Data flow between states
- Complex agent orchestration

### C# Coding Agent

**Files:** `Demos/CSharpCodingAgent/`

An agent that can write, compile, and test C# code.

**Components:**
- `CodeGenerationState.cs` - Generates C# code
- `CSharpBuildState.cs` - Compiles code
- `CodeReviewerState.cs` - Reviews and improves code

**Key Concepts:**
- Code generation
- Compilation integration
- Multi-step code development
- Quality assurance workflows

### Computer Use Preview

**Files:** `Demos/OpenAIComputerUsePreview/`

Advanced computer use scenarios with OpenAI models.

**Components:**
- `WindowsComputerUseExample.cs` - Windows-specific automation

**Key Concepts:**
- Advanced computer control
- Platform-specific implementations
- Computer vision integration

## Utility Examples

### CodeUtility

**File:** `Demos/Utility/CodeUtility.cs`

Helper functions for code compilation and execution.

```csharp
public static CompileResult CompileCode(string code, bool isTopLevel = false)
public static string ExecuteCode(string executablePath)
```

**Key Concepts:**
- Dynamic code compilation
- Process execution
- Error handling

### CommandLineUtility

**File:** `Demos/Utility/CommandLineUtility.cs`

Utilities for running command-line operations.

**Key Concepts:**
- Process management
- Command execution
- Output capture

## Running the Examples

### Prerequisites

1. Set environment variables:
```bash
export OPENAI_API_KEY="your-openai-key"
```

2. Build the solution:
```bash
dotnet build
```

### Running Individual Examples

Most examples are implemented as NUnit tests:

```bash
dotnet test --filter "TestName~BasicHelloWorld"
```

### Running All Examples

```bash
cd Examples
dotnet test
```

**Note:** Many examples will fail without proper API keys configured.

## Example Patterns and Best Practices

### 1. Agent Creation Pattern

```csharp
// Standard pattern for creating agents
Agent agent = new Agent(
    client: new OpenAIModelClient("model-name"),
    name: "Agent Name",
    instructions: "Clear instructions for behavior",
    outputSchema: typeof(OutputType),  // Optional
    tools: [tool1, tool2]             // Optional
);
```

### 2. State Machine Pattern

```csharp
// Create states
var state1 = new FirstState();
var state2 = new SecondState();

// Setup transitions
state1.AddTransition(condition, state2);
state2.AddTransition(_ => true, new ExitState());

// Create and run state machine
StateMachine<InputType, OutputType> sm = new();
sm.SetEntryState(state1);
sm.SetOutputState(state2);

var results = await sm.Run(input);
```

### 3. Tool Definition Pattern

```csharp
[Tool(
    Description = "Clear description of what the tool does",
    In_parameters_description = [
        "Parameter 1 description",
        "Parameter 2 description"
    ]
)]
public ReturnType ToolMethod(ParamType1 param1, ParamType2 param2 = defaultValue)
{
    // Implementation
    return result;
}
```

### 4. Error Handling Pattern

```csharp
try
{
    RunResult result = await Runner.RunAsync(agent, input);
    // Process successful result
}
catch (GuardRailTriggerException ex)
{
    // Handle guard rail violations
}
catch (Exception ex)
{
    // Handle other errors
}
```

## Customization Examples

### Custom Model Provider

```csharp
public class CustomModelClient : ModelClient
{
    public override async Task<ModelResponse> _CreateResponseAsync(
        List<ModelItem> messages, 
        ModelResponseOptions options)
    {
        // Custom implementation
    }
}
```

### Custom State Types

```csharp
public class CustomState : BaseState<CustomInput, CustomOutput>
{
    public override async Task<CustomOutput> Invoke(CustomInput input)
    {
        // Custom processing logic
        return new CustomOutput();
    }
}
```

## Performance Considerations

### Threading in State Machines

```csharp
// Configure thread limits for performance
StateMachine sm = new() { MaxThreads = 10 };

// Enable parallel processing in states
public class ParallelState : BaseState<Input, Output>
{
    public ParallelState() 
    { 
        AllowsParallelTransitions = true; 
    }
}
```

### Resource Management

```csharp
// Use CombineInput for batch processing
public class BatchProcessingState : BaseState<List<Item>, ProcessedResult>
{
    public BatchProcessingState() 
    { 
        CombineInput = true; 
    }
}
```

These examples provide a comprehensive foundation for building sophisticated AI agent applications with LombdaAgentSDK.