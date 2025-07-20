# Architecture Guide

This document outlines the architecture and design principles of the LombdaAgentSDK.

## High-Level Architecture
User → Agent → Model Provider → Response → Tool Execution → Output
LombdaAgentSDK follows a modular architecture with several key components:

1. **Agent**: Encapsulates LLM interactions and tool execution
2. **State Machine**: Manages workflow transitions between states
3. **LombdaAgent**: Orchestrator connecting agents and state machines
4. **Model Providers**: Abstract interfaces to various LLM services
5. **Tools**: Extensible functions that agents can call
6. **Event System**: Communication channel between components

## Agent Architecture

### Agent Design

An agent consists of:

- **ModelClient**: Connection to LLM service
- **Instructions**: System prompt for the agent
- **Tools**: Functions the agent can call
- **Output Schema**: Structure for response parsing
- **Conversation History**: Maintained message log

### Execution Flow
Input → Agent → Model → Tool Calls? → Execute Tools → Collect Responses → Final Output
### Tool System

Tools follow a standardized pattern:

- Decorated with `[Tool]` attributes
- Metadata for descriptions and parameters
- Auto-converted to tool specifications
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
Entry State → Process → Transition Check → Next State → ... → Exit State
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

## AgentStateMachine Architecture

AgentStateMachine extends the basic state machine architecture with agent-specific capabilities:
LombdaAgent → AgentStateMachine → AgentStates → Agent Execution → Events
### AgentState Design

AgentState adds agent-specific features to BaseState:

- **StateAgent**: Agent instance owned by the state
- **Event Handling**: Built-in event integration
- **Lifecycle Management**: Initialization and disposal
- **BeginRunnerAsync**: Helper for agent execution

### Flow in AgentStateMachine
Initialize → Setup States → Configure Transitions → Connect Events → Run → Monitor → Complete
1. **Initialization**: LombdaAgent creates AgentStateMachine
2. **State Setup**: AgentStates are created and configured
3. **Event Binding**: Events are connected to LombdaAgent
4. **Execution**: States process input and generate outputs
5. **Monitoring**: Events flow back to LombdaAgent for logging/debugging
6. **Completion**: Results are collected and returned

## LombdaAgent Architecture

LombdaAgent serves as the central coordination point:
Application → LombdaAgent → State Machines → States → Agents → Events → UI/Logging
### Responsibilities:

- **State Machine Management**: Tracks active state machines
- **Event Coordination**: Routes events between components
- **Logging**: Centralizes verbose logging
- **Streaming**: Manages streaming callbacks
- **Debugging**: Connects to UI components

### Communication Flow
Agent Action → Event → LombdaAgent → Event Handlers → UI/Logging
## Event System Architecture

The event system provides real-time communication between components:
Source → Event → Subscribers → Actions
### Event Types:

- **VerboseCallback**: Detailed operation logs
- **StreamingCallback**: Real-time model responses
- **RunningVerboseCallback**: High-level verbose messages
- **RunningStreamingCallback**: High-level streaming updates
- **OnStateEntered/OnStateExited**: State transition events
- **OnBegin/FinishedTriggered/CancellationTriggered**: Lifecycle events

### Event Flow:

1. **Source Generation**: Component raises event
2. **LombdaAgent Routing**: Central handler processes event
3. **Subscription Delivery**: Subscribers receive notifications
4. **UI/Logging Update**: Visual or text representation

## Model Provider Architecture

### Provider Abstraction

The `ModelClient` abstract class provides a uniform interface regardless of the underlying model provider:
public abstract class ModelClient
{
    public abstract Task<ModelResponse> _CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options);
    public abstract Task<ModelResponse> _CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks callback);
}
### Supported Providers

- **OpenAIModelClient**: Direct integration with OpenAI API
- **LLMTornadoModelProvider**: Multi-provider support through LLMTornado
- **Extensible**: Easy to add new providers by implementing ModelClient

## Data Flow Architecture

### Message Flow
User Input → ModelMessageItem → Conversation History → Model Provider → ModelResponse → Tool Calls → Output
### Type Safety

The SDK maintains strong typing throughout:
- **Generic States**: `BaseState<TInput, TOutput>` ensures type safety
- **Structured Output**: Automatic JSON schema generation from C# types
- **Tool Parameters**: Automatic parameter extraction and validation

## Debugging UI Architecture

The Windows debugging UI provides visualization of agent operations:
LombdaAgent → Events → UI → User
### UI Components:

- **Left Panel**: Streaming chat display
- **Right Panel**: Verbose logging display
- **Controls**: Operations like cancellation
- **State Visualization**: Visual representation of state machine

### Data Flow:

1. **Event Generation**: LombdaAgent raises events
2. **UI Update**: UI components receive and display events
3. **User Interaction**: Debugging controls affect execution
4. **Visualization**: State machine status is displayed

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
- IsDeadEnd state detection to prevent failed state rerunning

## Memory and State Management

### Conversation Memory
- Messages stored in `List<ModelItem>`
- Persistent across agent turns
- Includes tool calls and responses
- SharedModelItems for cross-state conversation history

### State Machine Memory
- `RuntimeProperties` for cross-state data
- State-specific input/output storage
- Process isolation for parallel execution
- Event history for debugging and monitoring

### Resource Management
- Automatic cleanup of completed states
- Configurable thread limits
- Memory-efficient message handling
- Cancellation support via CancellationTokenSource

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