# LombdaAgentSDK Architecture Guide

## Overview

LombdaAgentSDK is built around three core architectural concepts:

1. **Agents** - AI-powered entities that can process tasks, use tools, and interact with users
2. **State Machines** - Workflow orchestration system for complex multi-step processes  
3. **Model Providers** - Abstracted interfaces to different AI model services

## Core Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     Agents      │    │  State Machine  │    │ Model Providers │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │   Runner    │ │    │ │    State    │ │    │ │   OpenAI    │ │
│ │             │ │    │ │             │ │    │ │             │ │
│ │ ┌─────────┐ │ │    │ │ ┌─────────┐ │ │    │ │             │ │
│ │ │  Agent  │ │ │    │ │ │  Input  │ │ │    │ │             │ │
│ │ │         │ │ │    │ │ │         │ │ │    │ │             │ │
│ │ │ ┌─────┐ │ │ │    │ │ │ ┌─────┐ │ │ │    │ │             │ │
│ │ │ │Tools│ │ │ │    │ │ │ │Invoke│ │ │ │    │ │             │ │
│ │ │ └─────┘ │ │ │    │ │ │ └─────┘ │ │ │    │ │             │ │
│ │ └─────────┘ │ │    │ │ │         │ │ │    │ │             │ │
│ └─────────────┘ │    │ │ │ ┌─────┐ │ │ │    │ │             │ │
│                 │    │ │ │ │Output│ │ │ │    │ │             │ │
└─────────────────┘    │ │ │ └─────┘ │ │ │    │ │             │ │
                       │ │ └─────────┘ │ │    │ │             │ │
                       │ └─────────────┘ │    │ └─────────────┘ │
                       │                 │    │                 │
                       │ ┌─────────────┐ │    │ ┌─────────────┐ │
                       │ │ Transitions │ │    │ │ LLMTornado  │ │
                       │ └─────────────┘ │    │ └─────────────┘ │
                       └─────────────────┘    └─────────────────┘
```

## Agent Architecture

### Agent Composition

An **Agent** consists of:

- **ModelClient**: Interface to AI model provider (OpenAI, LLMTornado, etc.)
- **Instructions**: System prompt defining agent behavior
- **Tools**: Available functions the agent can call
- **OutputSchema**: Optional structured output format
- **Options**: Configuration for model parameters, guardrails, etc.

### Agent Execution Flow

```
User Input → Runner → Agent → Model Provider → Response Processing → Tool Calls → Final Output
```

1. **Input Processing**: User input is validated and added to conversation history
2. **Model Invocation**: Agent sends messages to model provider with tools and instructions
3. **Response Processing**: Model response is parsed for tool calls, content, and structured data
4. **Tool Execution**: Any tool calls are executed and results added back to conversation
5. **Loop Continuation**: Process repeats until no more tool calls or max turns reached

### Tool System

Tools are C# methods decorated with the `[Tool]` attribute:

```csharp
[Tool(Description = "Get weather data", In_parameters_description = ["Location", "Unit"])]
public string GetWeather(string location, Unit unit = Unit.celsius)
{
    // Implementation
}
```

Tools are automatically:
- Converted to OpenAI function call format
- Registered with the agent
- Executed when called by the model
- Results fed back into conversation

## State Machine Architecture

### State Design

States follow a generic pattern `BaseState<TInput, TOutput>` where:

- **TInput**: Type of data the state accepts
- **TOutput**: Type of data the state produces
- **Invoke()**: Core logic that transforms input to output
- **Transitions**: Conditions for moving to next states

### State Machine Flow

```
Entry State → Process → Transition Check → Next State → ... → Exit State
```

1. **Initialization**: Entry state receives initial input
2. **Processing**: State executes its `Invoke()` method
3. **Transition Evaluation**: Conditions are checked against output
4. **State Creation**: New state processes are created for valid transitions
5. **Parallel Execution**: Multiple states can run concurrently
6. **Coordination**: State machine manages thread safety and synchronization

### Thread Safety

State machines handle concurrency through:
- **SemaphoreSlim** for state access control
- **Thread limits** for maximum concurrent states
- **Process isolation** between state instances
- **Safe transition** mechanisms

## Model Provider Architecture

### Provider Abstraction

The `ModelClient` abstract class provides a uniform interface regardless of the underlying model provider:

```csharp
public abstract class ModelClient
{
    public abstract Task<ModelResponse> _CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options);
    public abstract Task<ModelResponse> _CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks callback);
}
```

### Supported Providers

- **OpenAIModelClient**: Direct integration with OpenAI API
- **LLMTornadoModelProvider**: Multi-provider support through LLMTornado
- **Extensible**: Easy to add new providers by implementing ModelClient

## Data Flow Architecture

### Message Flow

```
User Input → ModelMessageItem → Conversation History → Model Provider → ModelResponse → Tool Calls → Output
```

### Type Safety

The SDK maintains strong typing throughout:
- **Generic States**: `BaseState<TInput, TOutput>` ensures type safety
- **Structured Output**: Automatic JSON schema generation from C# types
- **Tool Parameters**: Automatic parameter extraction and validation

## Design Principles

### 1. Modularity
- Components are loosely coupled
- Easy to swap model providers
- Tools are self-contained
- States are independent units

### 2. Type Safety
- Strong typing throughout the system
- Compile-time validation of state connections
- Automatic schema generation

### 3. Extensibility
- Plugin architecture for tools
- Abstract base classes for extension
- Event-driven callbacks for customization

### 4. Concurrent Execution
- Thread-safe state machines
- Parallel state processing
- Configurable thread limits

### 5. Error Handling
- Graceful failure recovery
- Retry mechanisms
- Guard rail system for input validation

## Memory and State Management

### Conversation Memory
- Messages stored in `List<ModelItem>`
- Persistent across agent turns
- Includes tool calls and responses

### State Machine Memory
- `RuntimeProperties` for cross-state data
- State-specific input/output storage
- Process isolation for parallel execution

### Resource Management
- Automatic cleanup of completed states
- Configurable thread limits
- Memory-efficient message handling

## Security Considerations

### Input Validation
- Guard rail system for dangerous inputs
- Type validation for structured data
- Sanitization of tool parameters

### Tool Security
- Explicit tool registration required
- Sandboxed tool execution
- Parameter validation and type checking

### Model Provider Security
- API key management through providers
- Request/response logging capabilities
- Rate limiting support through providers