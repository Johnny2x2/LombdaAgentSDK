# Table of Contents

## LombdaAgentSDK Documentation

### Getting Started
- [Overview](README.md) - Documentation index and overview
- [Getting Started Guide](GETTING_STARTED.md) - Step-by-step tutorial for new users

### Reference Documentation  
- [API Reference](API_REFERENCE.md) - Complete API documentation for all classes and methods
- [Architecture Guide](ARCHITECTURE.md) - Deep dive into the SDK's design and architecture

### Examples and Tutorials
- [Examples Documentation](EXAMPLES.md) - Detailed overview of example projects and use cases

### Contributing
- [Contributing Guide](CONTRIBUTING.md) - Guidelines for contributors, development setup, and coding standards

## Quick Links

### Core Concepts
- **Agents** - AI-powered entities that process tasks and use tools
- **State Machines** - Workflow orchestration for complex multi-step processes  
- **Model Providers** - Abstracted interfaces to different AI model services
- **Tools** - Function calling system with automatic parameter extraction

### Main Classes
- `Agent` - Core agent class for creating AI agents
- `Runner` - Static class for executing agent workflows
- `BaseState<TInput, TOutput>` - Base class for state machine states
- `StateMachine<TInput, TOutput>` - Orchestrates state execution
- `ModelClient` - Abstract base for model providers

### Supported Providers
- `OpenAIModelClient` - Direct OpenAI integration
- `LLMTornadoModelProvider` - Multi-provider support

### Key Features
- ✅ Simple agent abstractions
- ⚙️ Powerful state machine workflows
- 🔍 Plug-and-play tool system
- 📦 .NET Standard compatible
- 🚀 Thread-safe and production ready