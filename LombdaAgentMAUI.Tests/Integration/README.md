# LombdaAgent MAUI Live Integration Tests

This directory contains tests that can run against a real LombdaAgent API server.

## Quick Start

### 1. Start your LombdaAgent API server

```bash
# From the solution root directory
cd LombdaAgentAPI
dotnet run
```

The API should start on `https://localhost:5001` by default.

### 2. Run the live integration tests

```bash
# Run all live integration tests
dotnet test --filter "Category=Network" --verbosity normal

# Run only the live server tests
dotnet test --filter "ClassName=LiveServerIntegrationTests" --verbosity normal

# Run a specific test
dotnet test --filter "TestName=LiveServer_SendMessage_ReturnsValidResponse" --verbosity normal
```

## Environment Configuration

### Environment Variables

Set these environment variables to customize test behavior:

```bash
# API URL (defaults to https://localhost:5001/)
set LOMBDA_TEST_API_URL=https://localhost:5001/

# Test environment (Local, Development, Staging, Production)
set LOMBDA_TEST_ENVIRONMENT=Local

# Enable live tests (set to true or 1)
set LOMBDA_RUN_LIVE_TESTS=true

# OpenAI API Key (required for the API to work)
set OPENAI_API_KEY=your-openai-api-key-here
```

### PowerShell Examples

```powershell
# Set environment variables in PowerShell
$env:LOMBDA_TEST_API_URL = "https://localhost:5001/"
$env:LOMBDA_RUN_LIVE_TESTS = "true"
$env:OPENAI_API_KEY = "your-openai-api-key-here"

# Run the tests
dotnet test --filter "Category=Network" --verbosity normal
```

### Command Prompt Examples

```cmd
# Set environment variables in Command Prompt
set LOMBDA_TEST_API_URL=https://localhost:5001/
set LOMBDA_RUN_LIVE_TESTS=true
set OPENAI_API_KEY=your-openai-api-key-here

# Run the tests
dotnet test --filter "Category=Network" --verbosity normal
```

## Test Categories

### Network Tests
Tests that require network access and a running API server:

```bash
dotnet test --filter "Category=Network" --verbosity normal
```

### Integration Tests
Tests that verify multiple components working together:

```bash
dotnet test --filter "Category=Integration" --verbosity normal
```

### Slow Tests
Tests that involve AI processing and may take longer:

```bash
# Run slow tests with extended timeout
dotnet test --filter "Category=Slow" --verbosity normal -- NUnit.DefaultTimeout=300000
```

### Skip Slow Tests
Skip time-consuming tests for faster feedback:

```bash
dotnet test --filter "Category!=Slow" --verbosity normal
```

## Individual Test Scenarios

### 1. Basic Connectivity Test
```bash
dotnet test --filter "TestName=LiveServer_GetAgents_ReturnsAgentList" --verbosity normal
```

### 2. Agent Creation Test
```bash
dotnet test --filter "TestName=LiveServer_CreateAgent_ReturnsValidAgent" --verbosity normal
```

### 3. Chat Functionality Test
```bash
dotnet test --filter "TestName=LiveServer_SendMessage_ReturnsValidResponse" --verbosity normal
```

### 4. Streaming Test
```bash
dotnet test --filter "TestName=LiveServer_StreamingMessage_ReceivesIncrementalResponses" --verbosity normal
```

### 5. Complete Workflow Test
```bash
dotnet test --filter "TestName=LiveServer_CompleteWorkflow_CreateAgentAndChat" --verbosity normal
```

## Test Output

The tests provide detailed console output showing:

- ? Successful operations
- ?? Messages sent to agents
- ?? Responses received from agents
- ?? Streaming chunks received
- ?? Performance metrics
- ?? Cleanup operations

## Troubleshooting

### Common Issues

#### 1. Connection Refused
```
? Cannot connect to LombdaAgent API server at https://localhost:5001/
```

**Solution:** Make sure the LombdaAgent API is running:
```bash
cd LombdaAgentAPI
dotnet run
```

#### 2. HTTPS Certificate Issues
```
?? HTTPS connection failed, trying HTTP fallback...
```

**Solution:** The tests automatically try HTTP fallback. You can also set:
```bash
set LOMBDA_TEST_API_URL=http://localhost:5000/
```

#### 3. OpenAI API Key Missing
The API requires an OpenAI API key to function. Make sure it's set:
```bash
set OPENAI_API_KEY=your-openai-api-key-here
```

#### 4. Timeout Issues
For slower networks or systems, increase timeout:
```bash
# Set longer timeout (5 minutes = 300000ms)
dotnet test --filter "Category=Network" -- NUnit.DefaultTimeout=300000
```

### Debug Mode

Run tests with detailed logging:
```bash
dotnet test --filter "Category=Network" --verbosity diagnostic
```

### Test One by One

Run tests individually to isolate issues:
```bash
# Test basic connectivity first
dotnet test --filter "TestName=LiveServer_GetAgents_ReturnsAgentList"

# Then test agent creation
dotnet test --filter "TestName=LiveServer_CreateAgent_ReturnsValidAgent"

# Then test messaging
dotnet test --filter "TestName=LiveServer_SendMessage_ReturnsValidResponse"
```

## Test Cleanup

The tests create temporary agents with names like:
- `TestAgent_20231201_143022_abc12345`
- `ChatTestAgent_20231201_143045_def67890`

These are tracked and noted for cleanup, but the current API doesn't support agent deletion yet.

## CI/CD Integration

For automated testing in CI/CD pipelines:

```yaml
# Example GitHub Actions step
- name: Run Live Integration Tests
  env:
    LOMBDA_TEST_API_URL: ${{ secrets.LOMBDA_TEST_API_URL }}
    LOMBDA_RUN_LIVE_TESTS: true
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  run: |
    # Start API server in background
    cd LombdaAgentAPI
    dotnet run &
    sleep 30  # Wait for startup
    
    # Run tests
    cd ../LombdaAgentMAUI.Tests
    dotnet test --filter "Category=Network" --verbosity normal
```

## Performance Expectations

Typical test execution times:
- Basic connectivity: < 5 seconds
- Agent creation: < 10 seconds  
- Simple chat message: 10-30 seconds
- Streaming messages: 15-45 seconds
- Complete workflow: 30-60 seconds

Times may vary based on:
- Network latency
- API server performance
- OpenAI API response times
- System resources