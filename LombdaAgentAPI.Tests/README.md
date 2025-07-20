# LombdaAgentAPI Tests

This project contains unit and integration tests for the LombdaAgentAPI.

## Overview

The test project is structured as follows:

- **Integration/**: Integration tests for API endpoints
  - **TestBase.cs**: Base class for all integration tests
  - **AgentsControllerTests.cs**: Tests for the Agents controller endpoints
  - **StreamControllerTests.cs**: Tests for streaming functionality
  - **AgentHubTests.cs**: Tests for SignalR hub functionality
  - **MockLombdaAgentService.cs**: Mock service implementation for tests

- **Unit/**: Unit tests for components
  - **LombdaAgentServiceTests.cs**: Tests for the LombdaAgentService

- **Mocks/**: Mock implementations for testing
  - **MockLombdaAgent.cs**: A test double for LombdaAgent

## Running Tests

To run the tests, use one of the following methods:

1. Using Visual Studio:
   - Open the solution in Visual Studio
   - Open the Test Explorer window
   - Run all tests or select specific tests to run

2. Using the command line:
   ```
   dotnet test LombdaAgentAPI.Tests/LombdaAgentAPI.Tests.csproj
   ```

## Test Strategy

### Integration Tests

Integration tests use `Microsoft.AspNetCore.Mvc.Testing` to create a test server that hosts the API. These tests make HTTP requests to the API endpoints and verify the responses, ensuring that the API behaves correctly.

For testing streaming functionality, which typically requires a persistent connection, the tests verify that the server properly sets up the connection and sends the correct headers, but don't verify the entire content stream.

### Unit Tests

Unit tests focus on testing individual components in isolation. Dependencies are mocked using Moq to ensure that tests only verify the behavior of the component being tested.

### Mocks

- **MockLombdaAgent**: Provides a test implementation of `LombdaAgent` that doesn't make real API calls to OpenAI.
- **MockModelClient**: A test implementation of `IModelClient` that returns pre-defined responses.
- **MockLombdaAgentService**: A test implementation of `ILombdaAgentService` that uses the mock agent.

## Adding New Tests

When adding new tests:

1. For new API endpoints, add integration tests in the appropriate controller test class.
2. For new components, add unit tests that verify the component's behavior in isolation.
3. If needed, extend the mock implementations to support the new functionality.

## Test Coverage

The tests cover:

- Creating, retrieving, and listing agents
- Sending messages to agents
- Stream-based communication
- SignalR real-time communication
- Error handling