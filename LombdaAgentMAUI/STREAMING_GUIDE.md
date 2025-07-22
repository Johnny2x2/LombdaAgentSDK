# Streaming Integration Guide for MAUI

## Updated Streaming System

The MAUI application has been updated to work with the new `ModelStreamingEvents` system. Here's what has changed:

### 1. Enhanced AgentApiService

The `AgentApiService` now includes a new method `SendMessageStreamWithEventsAsync` that provides detailed event information:

```csharp
public async Task<string?> SendMessageStreamWithEventsAsync(
    string agentId, 
    string message, 
    string? threadId, 
    Action<string> onMessageReceived, 
    Action<StreamingEventData>? onEventReceived = null, 
    CancellationToken cancellationToken = default)
```

### 2. Streaming Event Types

The new system provides the following event types:

- **connected**: Initial connection established
- **created**: Stream creation with response ID
- **delta**: Text chunks as they arrive
- **stream_complete**: Individual stream completion
- **complete**: Final response with thread ID
- **reasoning**: AI reasoning steps (for compatible models)
- **error/stream_error**: Error events

### 3. Enhanced MAUI Integration

The MainPage now includes:

- Enhanced logging for debugging streaming events
- Event-specific handling with visual feedback
- Better error handling and status updates
- Auto-scrolling during streaming

### 4. Testing

Updated `StreamControllerTests.cs` includes:

- Tests for the new event structure
- Verification of proper SSE format
- Event sequence validation
- JSON response validation

## Usage Example

```csharp
// In your MAUI page
private async Task SendStreamingMessage(string message)
{
    var resultThreadId = await _agentApiService.SendMessageStreamWithEventsAsync(
        agentId,
        message,
        currentThreadId,
        (streamedText) => {
            // Handle text chunks
            UpdateMessageUI(streamedText);
        },
        (eventData) => {
            // Handle specific events
            switch (eventData.EventType)
            {
                case "connected":
                    ShowStatus("Connected to stream");
                    break;
                case "delta":
                    LogDebug($"Received text chunk: {eventData.Text}");
                    break;
                case "complete":
                    ShowStatus($"Completed - Thread: {eventData.ThreadId}");
                    break;
            }
        },
        cancellationToken
    );
}
```

## Debugging

The enhanced logging system provides detailed information:

- Connection status
- Event sequence numbers
- Text chunk sizes
- Error details
- Performance metrics

Check the system log in the MAUI app for detailed debugging information during streaming operations.

## Compatibility

The old streaming methods (`SendMessageStreamAsync` and `SendMessageStreamWithThreadAsync`) are still available and will work with the new system, but they won't provide the enhanced event information.