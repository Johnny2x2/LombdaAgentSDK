# LombdaAgentSDK Documentation Index

Welcome to the comprehensive documentation for LombdaAgentSDK - a lightweight C# SDK for creating and running modular AI agents with state machine workflows.

## Quick Navigation

### 🚀 Get Started
- **[Getting Started Guide](GETTING_STARTED.md)** - Step-by-step tutorial for new users
- **[Installation](#installation)** - How to install and set up the SDK
- **[Quick Examples](#quick-examples)** - Simple code examples to get you started

### 📚 Documentation
- **[API Reference](API_REFERENCE.md)** - Complete API documentation for all classes and methods
- **[Architecture Guide](ARCHITECTURE.md)** - Deep dive into the SDK's design and architecture
- **[Examples Documentation](EXAMPLES.md)** - Detailed overview of example projects

### 🤝 Contributing
- **[Contributing Guide](CONTRIBUTING.md)** - How to contribute to the project
- **[Code Style Guidelines](CONTRIBUTING.md#code-style-and-standards)** - Coding standards and conventions

## Installation

### NuGet Package
```bash
dotnet add package LombdaAiAgents
```

### From Source
```bash
git clone https://github.com/Johnny2x2/LombdaAgentSDK.git
dotnet add reference path/to/LombdaAgentSDK/LombdaAgentSDK.csproj
```

## Quick Examples

### Simple Agent
```csharp
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;

Agent agent = new Agent(new OpenAIModelClient("gpt-4o-mini"), "Assistant", "You are helpful");
RunResult result = await Runner.RunAsync(agent, "Hello, world!");
Console.WriteLine(result.Text);
```

### Agent with Tools
```csharp
[Tool(Description = "Get current weather")]
public string GetWeather(string location) => $"Weather in {location}: 72°F, sunny";

Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"), 
    "Weather Bot", 
    "Help with weather",
    _tools: [GetWeather]
);
```

### Structured Output
```csharp
public struct WeatherReport
{
    public string location { get; set; }
    public int temperature { get; set; }
    public string conditions { get; set; }
}

Agent agent = new Agent(
    new OpenAIModelClient("gpt-4o-mini"),
    "Weather Analyzer",
    "Analyze weather data",
    _output_schema: typeof(WeatherReport)
);

RunResult result = await Runner.RunAsync(agent, "Analyze weather in Miami");
WeatherReport report = result.ParseJson<WeatherReport>();
```

### State Machine Workflow
```csharp
// Create states
var planState = new PlanningState();
var researchState = new ResearchState();
var reportState = new ReportState();

// Setup transitions
planState.AddTransition(plan => plan != null, researchState);
researchState.AddTransition(_ => true, reportState);
reportState.AddTransition(_ => true, new ExitState());

// Run workflow
StateMachine<string, string> workflow = new();
workflow.SetEntryState(planState);
workflow.SetOutputState(reportState);

var results = await workflow.Run("Research electric bikes under $1500");
```

## Core Concepts

### Agents
AI-powered entities that can:
- Process natural language inputs
- Execute tools and functions
- Maintain conversation context
- Produce structured outputs

### State Machines
Workflow orchestration system for:
- Multi-step processes
- Complex decision trees
- Parallel execution
- Data transformation pipelines

### Model Providers
Abstracted interfaces supporting:
- OpenAI models
- LLMTornado (multi-provider)
- Custom model implementations

### Tools
Function calling system with:
- Automatic parameter extraction
- Type safety
- Rich descriptions
- Agent composition

## Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     Agents      │    │  State Machine  │    │ Model Providers │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │   Runner    │ │◄──►│ │    State    │ │◄──►│ │   OpenAI    │ │
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

## Key Features

### ✅ Simple Abstractions
- Easy-to-use `Agent` and `State` classes
- Minimal setup required
- Clear, intuitive APIs

### ⚙️ Powerful Workflows
- Parallel state execution
- Conditional transitions
- Type-safe data flow
- Error handling and recovery

### 🔍 Plug-and-Play
- Modular tool system
- Swappable model providers
- Extensible architecture

### 📦 .NET Standard Compatible
- Works with .NET Framework and .NET Core
- Cross-platform support
- Modern C# features

### 🚀 Production Ready
- Thread-safe operations
- Resource management
- Comprehensive error handling
- Performance optimizations

## Use Cases

### Research Automation
Build agents that can:
- Plan research strategies
- Search multiple sources
- Synthesize findings
- Generate reports

### Code Generation
Create workflows for:
- Code generation
- Compilation and testing
- Code review
- Documentation generation

### Data Processing
Implement pipelines for:
- Data transformation
- Analysis and insights
- Report generation
- Quality assurance

### Customer Support
Build systems for:
- Query understanding
- Knowledge retrieval
- Response generation
- Escalation handling

## Advanced Features

### Streaming Support
```csharp
await Runner.RunAsync(agent, input, streaming: true, streamingCallback: Console.Write);
```

### Guard Rails
```csharp
await Runner.RunAsync(agent, input, guard_rail: ValidateInput);
```

### Computer Use
```csharp
await Runner.RunAsync(agent, input, computerUseCallback: HandleComputerAction);
```

### Parallel Processing
```csharp
state.AllowsParallelTransitions = true;
```

## Community and Support

### Getting Help
- **GitHub Issues** - Report bugs and request features
- **Discussions** - Ask questions and share ideas
- **Examples** - Learn from working code samples

### Contributing
- **Bug Reports** - Help improve reliability
- **Feature Requests** - Suggest new capabilities
- **Documentation** - Improve guides and examples
- **Code Contributions** - Add features and fixes

### Resources
- **[GitHub Repository](https://github.com/Johnny2x2/LombdaAgentSDK)**
- **[NuGet Package](https://www.nuget.org/packages/LombdaAiAgents)**
- **[Release Notes](https://github.com/Johnny2x2/LombdaAgentSDK/releases)**

## License

This project is licensed under the MIT License - see the [LICENSE.txt](../LICENSE.txt) file for details.

---

> [!CAUTION]
> This framework is built by Johnny2x2 who has never held a job as a developer. The purpose of this repo is to share ideas and spark discussion and for experienced devs to play with. Not meant for production use. Use with caution.

---

**Happy coding with LombdaAgentSDK! 🚀**