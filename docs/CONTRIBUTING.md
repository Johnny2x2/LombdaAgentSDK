# Contributing to LombdaAgentSDK

Thank you for your interest in contributing to LombdaAgentSDK! This guide will help you get started with contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Guidelines](#contributing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing](#testing)
- [Documentation](#documentation)
- [Reporting Issues](#reporting-issues)

## Code of Conduct

By participating in this project, you agree to abide by our code of conduct:

- Be respectful and inclusive
- Welcome newcomers and help them learn
- Focus on constructive feedback
- Respect different viewpoints and experiences

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Your favorite C# IDE (Visual Studio, VS Code, JetBrains Rider)
- OpenAI API key for testing (optional but recommended)

### Development Setup

1. **Fork the repository**
   ```bash
   # Fork on GitHub, then clone your fork
   git clone https://github.com/YOUR_USERNAME/LombdaAgentSDK.git
   cd LombdaAgentSDK
   ```

2. **Set up upstream remote**
   ```bash
   git remote add upstream https://github.com/Johnny2x2/LombdaAgentSDK.git
   ```

3. **Build the project**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```
   Note: Some tests require API keys and may fail without proper configuration.

5. **Set up environment variables**
   ```bash
   export OPENAI_API_KEY="your-api-key-here"
   ```

## Contributing Guidelines

### Types of Contributions

We welcome various types of contributions:

- **Bug fixes** - Fix issues in existing code
- **New features** - Add new functionality
- **Documentation** - Improve or add documentation
- **Examples** - Create new example applications
- **Tests** - Add or improve test coverage
- **Performance improvements** - Optimize existing code

### Before You Start

1. **Check existing issues** - Look for related issues or feature requests
2. **Create an issue** - For significant changes, create an issue first to discuss
3. **Start small** - Begin with small, focused contributions
4. **Follow conventions** - Maintain consistency with existing code

### Branching Strategy

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Keep branches focused** - One feature or fix per branch

3. **Use descriptive names**
   - `feature/add-anthropic-provider`
   - `fix/state-machine-deadlock`
   - `docs/improve-getting-started`

## Pull Request Process

### Before Submitting

1. **Update from upstream**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Test your changes**
   ```bash
   dotnet test
   dotnet build
   ```

3. **Check code style**
   - Follow existing code formatting
   - Use meaningful variable names
   - Add XML documentation for public APIs

### Creating the Pull Request

1. **Write a clear title**
   - `Add: Anthropic Claude model provider`
   - `Fix: State machine deadlock in parallel execution`
   - `Docs: Improve getting started guide`

2. **Provide detailed description**
   - What does this change do?
   - Why is this change needed?
   - How was it tested?
   - Any breaking changes?

3. **Link related issues**
   - `Fixes #123`
   - `Relates to #456`

### Review Process

1. **Automated checks** - Ensure all CI checks pass
2. **Code review** - Address reviewer feedback
3. **Testing** - Verify functionality works as expected
4. **Documentation** - Update docs if needed

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Test/Test.csproj

# Run with verbose output
dotnet test --verbosity normal
```

### Writing Tests

1. **Unit tests** - Test individual components
   ```csharp
   [Test]
   public void TestStateMachineTransition()
   {
       // Arrange
       var state = new TestState();
       
       // Act
       var result = state.CheckConditions();
       
       // Assert
       Assert.That(result.Count, Is.EqualTo(1));
   }
   ```

2. **Integration tests** - Test component interactions
   ```csharp
   [Test]
   public async Task TestAgentWithTools()
   {
       var agent = new Agent(mockClient, "Test", "Test instructions", _tools: [TestTool]);
       var result = await Runner.RunAsync(agent, "test input");
       Assert.That(result.Text, Is.Not.Empty);
   }
   ```

3. **Example tests** - Test example applications
   ```csharp
   [Test]
   public async Task TestBasicHelloWorld()
   {
       // Test that examples work correctly
   }
   ```

### Test Guidelines

- **Mock external dependencies** - Use mocks for API calls
- **Test edge cases** - Handle null inputs, errors, etc.
- **Keep tests focused** - One concept per test
- **Use descriptive names** - Test names should explain what they test

## Documentation

### Types of Documentation

1. **API Documentation** - XML comments for public APIs
   ```csharp
   /// <summary>
   /// Creates a new agent with the specified configuration
   /// </summary>
   /// <param name="client">Model provider client</param>
   /// <param name="name">Agent name</param>
   /// <returns>Configured agent instance</returns>
   public Agent(ModelClient client, string name)
   ```

2. **User Guides** - Markdown documentation in `docs/`
3. **Examples** - Working code examples in `Examples/`
4. **README updates** - Keep main README current

### Documentation Guidelines

- **Be clear and concise** - Use simple language
- **Include examples** - Show how to use features
- **Keep it current** - Update docs with code changes
- **Use proper formatting** - Follow markdown conventions

## Reporting Issues

### Bug Reports

When reporting bugs, include:

1. **Description** - What's the problem?
2. **Steps to reproduce** - How can we recreate it?
3. **Expected behavior** - What should happen?
4. **Actual behavior** - What actually happens?
5. **Environment** - OS, .NET version, etc.
6. **Code sample** - Minimal reproduction case

### Feature Requests

For feature requests, include:

1. **Use case** - Why is this needed?
2. **Proposed solution** - How should it work?
3. **Alternatives** - Other approaches considered?
4. **Examples** - Show how it would be used

### Issue Templates

```markdown
**Bug Report**
- Description: Brief description of the issue
- Steps to reproduce: 
  1. Step one
  2. Step two
- Expected: What should happen
- Actual: What actually happens
- Environment: OS, .NET version, SDK version
```

## Code Style and Standards

### C# Conventions

1. **Naming**
   - PascalCase for public members
   - camelCase for private members
   - UPPER_CASE for constants

2. **Formatting**
   - Use consistent indentation
   - Braces on new lines for methods/classes
   - Spaces around operators

3. **Comments**
   - XML documentation for public APIs
   - Inline comments for complex logic
   - Avoid obvious comments

### Architecture Principles

1. **Modularity** - Keep components loosely coupled
2. **Type Safety** - Use strong typing throughout
3. **Extensibility** - Design for future extension
4. **Performance** - Consider thread safety and resource usage

## Development Workflow

### Typical Workflow

1. **Pick an issue** - Choose from existing issues or create new one
2. **Create branch** - Feature branch from main
3. **Implement changes** - Write code, tests, documentation
4. **Test locally** - Ensure everything works
5. **Create PR** - Submit for review
6. **Address feedback** - Make requested changes
7. **Merge** - Once approved, changes are merged

### Communication

- **Be responsive** - Reply to comments and feedback
- **Ask questions** - Don't hesitate to ask for clarification
- **Be patient** - Reviews take time, especially for large changes

## Getting Help

- **Create an issue** - For questions about contributing
- **Check existing docs** - Review existing documentation
- **Look at examples** - See how existing code works
- **Ask maintainers** - Reach out to project maintainers

## Recognition

Contributors are recognized in:
- GitHub contributor list
- Release notes for significant contributions
- Project acknowledgments

Thank you for contributing to LombdaAgentSDK! 🚀