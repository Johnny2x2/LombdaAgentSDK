# LombdaAgentSDK Tests

This directory contains comprehensive NUnit tests for the LombdaAgentSDK, focusing on the LLM Tornado client implementation, tools functionality, and different output formats.

## Test Structure

### Core Tests
- **`Agents/AgentTests.cs`** - Tests for the Agent class including constructor scenarios, tool setup, output schemas, and initialization options
- **`LLMTornado/LLMTornadoModelProviderTests.cs`** - Tests for the LLMTornadoModelProvider covering constructor validation, configuration setup, and initialization scenarios
- **`Tools/ToolRunnerTests.cs`** - Tests for ToolRunner functionality covering various function types, async functions, agent tools, and error handling

### Data and Formats
- **`DataClasses/ModelDataClassesTests.cs`** - Tests for core data structures like ModelMessageItem, ModelResponse, ModelResponseOptions, etc.
- **`OutputFormats/StructuredOutputTests.cs`** - Tests for structured outputs covering simple and complex data structures, nested objects, arrays, and JSON schema generation

### Integration and Performance
- **`Integration/RunnerIntegrationTests.cs`** - Integration tests for the Runner class covering various scenarios including tool usage, structured outputs, and agent-as-tool chains
- **`Performance/PerformanceTests.cs`** - Performance tests to verify SDK performance under various conditions including timeout constraints and multiple tools

### Utilities and Error Handling
- **`Utilities/UtilityTests.cs`** - Tests for utility functions including function-to-tool conversion, JSON schema generation, and argument parsing
- **`ErrorHandling/ErrorHandlingTests.cs`** - Error handling and edge case tests covering null inputs, invalid API keys, malformed data, and exceptions

### Base Classes
- **`TestBase.cs`** - Base test class providing common functionality, configuration management, and helper methods

## Prerequisites

### Required Environment Variables
- **`OPENAI_API_KEY`** - Required for integration tests that make actual API calls

### NuGet Packages
The test project includes the following packages:
- `NUnit` - Testing framework
- `Moq` - Mocking framework
- `FluentAssertions` - Better assertion syntax
- `Microsoft.NET.Test.Sdk` - Test SDK
- `System.Text.Json` - JSON serialization for testing

## Running Tests

### All Tests
```bash
dotnet test Test/Test.csproj
```

### Specific Test Categories
```bash
# Run only unit tests (no API calls)
dotnet test Test/Test.csproj --filter "Category!=Performance"

# Run only performance tests
dotnet test Test/Test.csproj --filter "Category=Performance"

# Run integration tests
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Integration"
```

### Individual Test Classes
```bash
# Run LLM Tornado provider tests
dotnet test Test/Test.csproj --filter "FullyQualifiedName~LLMTornadoModelProviderTests"

# Run tool runner tests
dotnet test Test/Test.csproj --filter "FullyQualifiedName~ToolRunnerTests"

# Run structured output tests
dotnet test Test/Test.csproj --filter "FullyQualifiedName~StructuredOutputTests"
```

### With Coverage
```bash
dotnet test Test/Test.csproj --collect:"XPlat Code Coverage"
```

## Test Configuration

### Environment Setup
1. Set the `OPENAI_API_KEY` environment variable:
   ```bash
   # Windows
   set OPENAI_API_KEY=your_api_key_here
   
   # Linux/Mac
   export OPENAI_API_KEY=your_api_key_here
   ```

2. Tests that require API access will be automatically skipped if the API key is not available.

### Configuration File
The `appsettings.test.json` file contains test-specific configuration including:
- Test timeouts
- Default models
- Test prompts

## Test Categories

### Unit Tests
- Fast execution
- No external dependencies
- Test individual components in isolation
- Mock external dependencies

### Integration Tests
- Test complete workflows
- Make actual API calls (requires API key)
- Test real tool execution
- Verify end-to-end functionality

### Performance Tests
- Measure response times
- Test with multiple tools
- Verify streaming performance
- Check for memory leaks and performance degradation

### Error Handling Tests
- Test null inputs and edge cases
- Verify exception handling
- Test malformed data scenarios
- Validate graceful failure modes

## Key Test Scenarios

### LLM Tornado Client
- ? Constructor validation with various parameters
- ? Response API vs Chat API configuration
- ? Tool setup and conversion
- ? Streaming functionality
- ? Error handling for invalid configurations

### Tools Functionality
- ? Function-to-tool conversion
- ? Synchronous and asynchronous tool execution
- ? Tool parameter parsing and validation
- ? Agent-as-tool functionality
- ? Multiple tool selection and execution
- ? Error handling in tool execution

### Output Formats
- ? Simple structured outputs (objects with basic properties)
- ? Complex nested structures (arrays, dictionaries, nested objects)
- ? JSON schema generation from types
- ? Structured output parsing and validation
- ? Error handling for invalid JSON

### Agent Functionality
- ? Agent creation with various configurations
- ? Instruction handling
- ? Tool integration
- ? Output schema configuration
- ? MCP server integration

## Troubleshooting

### Common Issues

1. **Tests skipped due to missing API key**
   - Ensure `OPENAI_API_KEY` environment variable is set
   - Verify the API key is valid and has sufficient quota

2. **Performance tests timing out**
   - Check internet connection
   - Verify OpenAI API status
   - Increase timeout values in test configuration if needed

3. **Integration tests failing**
   - Check API key permissions
   - Verify the model being used is available
   - Check for API rate limiting

### Debug Mode
Run tests with detailed output:
```bash
dotnet test Test/Test.csproj --logger "console;verbosity=detailed"
```

## Contributing

When adding new tests:

1. Follow the existing naming conventions
2. Use the `TestBase` class for common functionality
3. Add appropriate test categories
4. Include both positive and negative test cases
5. Add performance considerations for integration tests
6. Update this README with new test scenarios

## Test Coverage Goals

The test suite aims to cover:
- ? **Constructor and initialization scenarios** - 95%+
- ? **Core functionality paths** - 90%+
- ? **Error handling scenarios** - 85%+
- ? **Integration workflows** - 80%+
- ? **Performance characteristics** - Basic coverage

## Notes

- Integration tests require an active internet connection and valid API credentials
- Some tests may take longer to run due to network latency
- Performance tests include timing assertions that may be affected by system load
- Tests are designed to be run independently and can be executed in any order