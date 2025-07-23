# LombdaAgent API

This API provides HTTP endpoints to interact with LombdaAgent instances. It supports both regular HTTP and Server-Sent Events (SSE) for streaming responses.

## Getting Started

1. Set environment variable for OpenAI API key:
   ```
   setx OPENAI_API_KEY "your-api-key-here"
   ```

2. Run the API project:
   ```
   dotnet run
   ```

3. API will be available at:
   - HTTP: https://localhost:5001
   - Swagger UI: https://localhost:5001/swagger

## API Endpoints

### Agents

#### GET /v1/agents
Returns a list of all available agent IDs.

#### POST /v1/agents
Creates a new agent.

Request body:
```json
{
  "name": "MyAssistant"
  "type": "Default"
}
```

Response:
```json
{
  "id": "agent-guid",
  "name": "MyAssistant"
}
```

#### GET /v1/agents/{id}
Gets information about a specific agent.

Response:
```json
{
  "id": "agent-guid",
  "name": "MyAssistant"
}
```

### Messages

#### POST /v1/agents/{id}/messages
Sends a message to an agent and returns the response.

Request body:
```json
{
  "text": "Hello, how are you?",
  "threadId": "optional-thread-id"
}
```

Response:
```json
{
  "agentId": "agent-guid",
  "threadId": "thread-guid",
  "text": "I'm doing well, thank you for asking. How can I assist you today?"
}
```

#### POST /v1/agents/{id}/messages/stream
Sends a message to an agent and returns the response as server-sent events.

Request body:
```json
{
  "text": "Hello, how are you?",
  "threadId": "optional-thread-id"
}
```

Response is a stream with events:
```
event: message
data: I'm doing well,

event: message
data: thank you for asking.

event: message
data: How can I assist you today?

event: complete
data: {
data: "threadId": "thread-guid",
data: "text": "I'm doing well, thank you for asking. How can I assist you today?"
data: }
```

### Streaming

#### GET /v1/stream/agents/{id}
Opens a continuous server-sent event stream for a specific agent.

Response is a stream with events:
```
event: message
data: [Any message from the agent's RootStreamingEvent]
```

## Using with JavaScript

```javascript
// Regular request
async function sendMessage(agentId, message, threadId = null) {
  const response = await fetch(`/v1/agents/${agentId}/messages`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      text: message,
      threadId: threadId
    })
  });
  return await response.json();
}

// Streaming request
function sendStreamingMessage(agentId, message, threadId = null) {
  const eventSource = new EventSource(`/v1/agents/${agentId}/messages/stream`);
  
  eventSource.addEventListener('message', (event) => {
    console.log('Received chunk:', event.data);
  });
  
  eventSource.addEventListener('complete', (event) => {
    const data = JSON.parse(event.data);
    console.log('Complete response:', data.text);
    console.log('Thread ID:', data.threadId);
    eventSource.close();
  });
  
  eventSource.addEventListener('error', (event) => {
    console.error('Error:', event);
    eventSource.close();
  });
  
  // Send the message
  fetch(`/v1/agents/${agentId}/messages/stream`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      text: message,
      threadId: threadId
    })
  });
}

// Continuous streaming
function subscribeToAgent(agentId) {
  const eventSource = new EventSource(`/v1/stream/agents/${agentId}`);
  
  eventSource.addEventListener('message', (event) => {
    console.log('Agent update:', event.data);
  });
  
  eventSource.addEventListener('error', (event) => {
    console.error('Stream error:', event);
    eventSource.close();
    // Implement reconnection logic if needed
  });
  
  return eventSource; // Keep reference to close later
}
```

## SignalR Support

The API also includes SignalR support for more robust real-time communication. Connect to the `/agentHub` endpoint and use the following methods:

- `SubscribeToAgent(agentId)`: Subscribe to an agent's streaming events
- `ReceiveAgentStream`: Event that will be sent from the server with agent streaming data