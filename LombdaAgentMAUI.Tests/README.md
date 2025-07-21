# LombdaAgentMAUI.Tests

Comprehensive test suite for the LombdaAgentMAUI cross-platform application.

## Overview

This test project provides thorough coverage of the LombdaAgentMAUI application including:

- **Unit Tests**: Testing individual components in isolation
- **Integration Tests**: Testing multiple components working together
- **Live Integration Tests**: Testing against a real LombdaAgent API server
- **Service Tests**: Testing API communication and configuration services
- **Model Tests**: Testing data models and their behavior

## Test Structure
LombdaAgentMAUI.Tests/
??? Services/
?   ??? ConfigurationServiceTests.cs    # Configuration service tests
?   ??? AgentApiServiceTests.cs         # API service tests
??? Models/
?   ??? ApiModelsTests.cs               # Data model tests
??? Integration/
?   ??? ServiceIntegrationTests.cs      # Mock-based integration tests
?   ??? LiveServerIntegrationTests.cs   # Real server integration tests
?   ??? README.md                       # Live testing documentation
??? Configuration/
?   ??? TestConfiguration.cs            # Test environment configuration
??? Helpers/
?   ??? TestDataFactory.cs              # Test data creation utilities
??? Mocks/
?   ??? MockSecureStorageService.cs     # Mock implementations
??? TestBase.cs                          # Base class for common test utilities
??? run-live-tests.ps1                  # PowerShell script for live tests
??? run-live-tests.bat                  # Batch script for live tests
??? README.md                           # This file
## Test Categories

Tests are organized using the following categories:

- **Unit**: Individual component tests (fast, no external dependencies)
- **Integration**: Multi-component interaction tests (with mocks)
- **Network**: Tests requiring network access and real API server
- **Api**: API-related functionality tests
- **Configuration**: Settings and configuration tests
- **Models**: Data model tests
- **Services**: Service layer tests
- **Slow**: Long-running tests (involve AI processing)

## Running Tests

### Quick Start - All Unit Tests# Fast unit tests only (no network required)
dotnet test --filter "Category!=Network" --verbosity normal
### Live Integration Tests (Requires Running API Server)

#### Prerequisites
1. **Start the LombdaAgent API server:**cd LombdaAgentAPI
dotnet run
2. **Set your OpenAI API key:**# PowerShell
$env:OPENAI_API_KEY = "your-openai-api-key-here"

# Command Prompt
set OPENAI_API_KEY=your-openai-api-key-here

# Linux/Mac
export OPENAI_API_KEY="your-openai-api-key-here"
#### Run Live Tests

**Using the helper scripts:**# PowerShell (recommended)
.\run-live-tests.ps1

# Command Prompt
run-live-tests.bat

# With options
.\run-live-tests.ps1 -SkipSlowTests
.\run-live-tests.ps1 -ApiUrl "http://localhost:5000/"
**Using dotnet test directly:**# All network tests
dotnet test --filter "Category=Network" --verbosity normal

# Skip slow tests (faster feedback)
dotnet test --filter "Category=Network&Category!=Slow" --verbosity normal

# Specific live server tests only
dotnet test --filter "ClassName=LiveServerIntegrationTests" --verbosity normal
### Other Test Scenarios
# All tests (unit + integration, but skip network)
dotnet test --filter "Category!=Network" --verbosity normal

# Specific categories
dotnet test --filter "Category=Unit" --verbosity normal
dotnet test --filter "Category=Models" --verbosity normal
dotnet test --filter "Category=Services" --verbosity normal

# Specific test classes
dotnet test --filter "ClassName=ConfigurationServiceTests" --verbosity normal
dotnet test --filter "ClassName=AgentApiServiceTests" --verbosity normal

# Individual tests
dotnet test --filter "TestName=LiveServer_CreateAgent_ReturnsValidAgent" --verbosity normal
## Live Integration Test Features

The live tests verify real-world scenarios:

### ? Basic Connectivity
- Server accessibility
- Agent listing
- Error handling

### ? Agent Management
- Create new agents
- Retrieve agent details
- Handle non-existent agents

### ? Chat Functionality
- Send messages to agents
- Receive AI-generated responses
- Maintain conversation context
- Thread management

### ? Streaming Support
- Real-time message streaming
- Incremental response handling
- Cancellation support

### ? Complete Workflows
- End-to-end agent creation and chat
- Error recovery
- Network timeout handling

## Environment Configuration

### Environment Variables

Set these to customize test behavior:
# API URL (defaults to https://localhost:5001/)
LOMBDA_TEST_API_URL=https://localhost:5001/

# Test environment (Local, Development, Staging, Production)
LOMBDA_TEST_ENVIRONMENT=Local

# Enable live tests
LOMBDA_RUN_LIVE_TESTS=true

# OpenAI API Key (required for API functionality)
OPENAI_API_KEY=your-openai-api-key-here
### Test Configuration Examples
# Test against different API URL
$env:LOMBDA_TEST_API_URL = "http://localhost:5000/"
dotnet test --filter "Category=Network"

# Test against staging environment
$env:LOMBDA_TEST_ENVIRONMENT = "Staging"
$env:LOMBDA_TEST_API_URL = "https://staging-api.example.com/"
dotnet test --filter "Category=Network"
## Test Output and Reporting

The tests provide rich console output:
?? Starting: LiveServer_SendMessage_ReturnsValidResponse
   ?? Testing real AI agent conversation

?? API Call: POST v1/agents
   ?? Request: {"name":"ChatTestAgent_20231201_143022"}
   ?? Response (145ms): {"id":"agent-abc123","name":"ChatTestAgent_20231201_143022"}

?? Sent: Hello! Please respond with a simple greeting.
?? Received: Hello! It's nice to meet you. How can I assist you today?
?? Thread ID: thread-def456

? Passed: LiveServer_SendMessage_ReturnsValidResponse
   ?? Response time: 2.3 seconds, Response length: 58 characters
## Performance Expectations

Typical test execution times:

| Test Type | Expected Duration |
|-----------|------------------|
| Unit tests | < 1 second each |
| Mock integration tests | 1-5 seconds each |
| Basic connectivity | < 5 seconds |
| Agent creation | < 10 seconds |
| Simple chat message | 10-30 seconds |
| Streaming messages | 15-45 seconds |
| Complete workflow | 30-60 seconds |

## Test Coverage

### Unit Tests (100% Coverage)
- **ConfigurationService**: Settings persistence and retrieval
- **AgentApiService**: HTTP communication logic
- **Models**: Data structures and validation

### Integration Tests (100% Coverage)
- **Service interactions**: Configuration + API service
- **Error handling**: Network failures, invalid responses
- **Concurrent operations**: Multiple simultaneous API calls

### Live Integration Tests
- **Real API communication**: Actual HTTP requests/responses
- **AI agent behavior**: Real OpenAI integration
- **End-to-end workflows**: Complete user scenarios
- **Performance testing**: Real-world timing and throughput

## Troubleshooting

### Common Issues

#### 1. API Connection Issues? Cannot connect to LombdaAgent API server at https://localhost:5001/
**Solutions:**
- Ensure API server is running: `cd LombdaAgentAPI && dotnet run`
- Check firewall settings
- Try HTTP instead: `$env:LOMBDA_TEST_API_URL = "http://localhost:5000/"`

#### 2. OpenAI API Key Issues
**Solution:** Set your OpenAI API key:$env:OPENAI_API_KEY = "sk-your-key-here"
#### 3. Slow Test Performance
**Solutions:**
- Skip slow tests: `.\run-live-tests.ps1 -SkipSlowTests`
- Increase timeout: `dotnet test -- NUnit.DefaultTimeout=300000`
- Check network connectivity

#### 4. HTTPS Certificate Issues
**Solution:** Use HTTP for local testing:$env:LOMBDA_TEST_API_URL = "http://localhost:5000/"
### Debug Mode

Run with detailed logging:dotnet test --filter "Category=Network" --verbosity diagnostic
### Test Isolation

Run tests individually to isolate problems:# Test connectivity first
dotnet test --filter "TestName=LiveServer_GetAgents_ReturnsAgentList"

# Test agent creation
dotnet test --filter "TestName=LiveServer_CreateAgent_ReturnsValidAgent"

# Test basic messaging
dotnet test --filter "TestName=LiveServer_SendMessage_ReturnsValidResponse"
## CI/CD Integration

Example GitHub Actions configuration:
name: Live Integration Tests

on: [push, pull_request]

jobs:
  live-tests:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Start API Server
      run: |
        cd LombdaAgentAPI
        dotnet run &
        sleep 30  # Wait for startup
      env:
        OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
    
    - name: Run Live Integration Tests
      run: |
        cd LombdaAgentMAUI.Tests
        dotnet test --filter "Category=Network&Category!=Slow" --verbosity normal
      env:
        LOMBDA_TEST_API_URL: "http://localhost:5000/"
        LOMBDA_RUN_LIVE_TESTS: "true"
        OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
## Contributing

When adding new tests:

1. **Unit tests**: Use mocks, should be fast and isolated
2. **Integration tests**: Use `ServiceIntegrationTests` for mock-based testing
3. **Live tests**: Add to `LiveServerIntegrationTests` for real server testing
4. **Follow naming**: `MethodName_Scenario_ExpectedResult`
5. **Add categories**: Use appropriate `[Category]` attributes
6. **Update documentation**: Update this README

### Test Template
[Test]
[Category(TestCategories.Network)]
[RequiresNetwork("Test requires running API server")]
public async Task LiveServer_YourFeature_ExpectedBehavior()
{
    // Arrange
    TestReporting.LogTestStart("YourFeature", "Description of what this tests");
    
    // Act
    var result = await _apiService.YourMethod();
    
    // Assert
    Assert.That(result, Is.Not.Null);
    
    TestReporting.LogTestSuccess("YourFeature", $"Details: {result}");
}
## Dependencies

- **NUnit 4.2.2**: Test framework
- **Moq 4.20.72**: Mocking framework  
- **Microsoft.NET.Test.Sdk**: Test SDK
- **Microsoft.Extensions.DependencyInjection**: Service container
- **System.Text.Json**: JSON serialization
- **LombdaAgentMAUI.Core**: Business logic library