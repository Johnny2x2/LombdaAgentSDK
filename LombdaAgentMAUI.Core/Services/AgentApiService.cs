using System.Text;
using System.Text.Json;
using LombdaAgentMAUI.Core.Models;

namespace LombdaAgentMAUI.Core.Services
{
    // Define the streaming events to match the API
    public class StreamingEventData
    {
        public string EventType { get; set; } = string.Empty;
        public int SequenceId { get; set; }
        public string? ResponseId { get; set; }
        public string? Text { get; set; }
        public string? Error { get; set; }
        public string? ThreadId { get; set; }
        public int OutputIndex { get; set; }
        public int ContentIndex { get; set; }
        public string? ItemId { get; set; }
    }

    public interface IAgentApiService
    {
        Task<List<string>> GetAgentsAsync();
        Task<List<string>> GetAgentTypesAsync();
        Task<AgentResponse?> CreateAgentAsync(string name, string agentType = "Default");
        Task<AgentResponse?> GetAgentAsync(string id);
        Task<MessageResponse?> SendMessageAsync(string agentId, string message, string? threadId = null);
        Task SendMessageStreamAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default);
        Task<string?> SendMessageStreamWithThreadAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Enhanced streaming method that provides detailed event information
        /// </summary>
        Task<string?> SendMessageStreamWithEventsAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, Action<StreamingEventData>? onEventReceived = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Update the base URL for API calls. This will create a new HttpClient instance.
        /// </summary>
        void UpdateBaseUrl(string baseUrl);
    }

    public class AgentApiService : IAgentApiService, IDisposable
    {
        private HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly bool _ownsHttpClient;

        public AgentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false; // DI-provided client, don't dispose
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        // Constructor for creating our own HttpClient instances
        private AgentApiService(string baseUrl)
        {
            _httpClient = CreateHttpClient(baseUrl);
            _ownsHttpClient = true; // We own this client, dispose it
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        private static HttpClient CreateHttpClient(string baseUrl)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
            return client;
        }

        public void UpdateBaseUrl(string baseUrl)
        {
            // If we own the current client, dispose it
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }

            // Create a new client with the new base URL
            _httpClient = CreateHttpClient(baseUrl);
        }

        public async Task<List<string>> GetAgentsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("v1/agents");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting agents: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetAgentTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("v1/agents/types");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting agent types: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<AgentResponse?> CreateAgentAsync(string name, string agentType = "Default")
        {
            try
            {
                var request = new AgentCreationRequest { Name = name, AgentType = agentType };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("v1/agents", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AgentResponse>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating agent: {ex.Message}");
                return null;
            }
        }

        public async Task<AgentResponse?> GetAgentAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"v1/agents/{id}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AgentResponse>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting agent: {ex.Message}");
                return null;
            }
        }

        public async Task<MessageResponse?> SendMessageAsync(string agentId, string message, string? threadId = null)
        {
            try
            {
                var request = new MessageRequest { Text = message, ThreadId = threadId };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"v1/agents/{agentId}/messages", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MessageResponse>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
                return null;
            }
        }

        // Legacy method - use Action-based implementation
        public async Task SendMessageStreamAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
        {
            await SendMessageStreamWithEventsAsync(agentId, message, threadId, onMessageReceived, null, cancellationToken);
        }

        // Legacy method - use Action-based implementation
        public async Task<string?> SendMessageStreamWithThreadAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
        {
            return await SendMessageStreamWithEventsAsync(agentId, message, threadId, onMessageReceived, null, cancellationToken);
        }

        // Action-based implementation (legacy compatibility)
        public async Task<string?> SendMessageStreamWithEventsAsync(
            string agentId, 
            string message, 
            string? threadId, 
            Action<string> onMessageReceived, 
            Action<StreamingEventData>? onEventReceived = null, 
            CancellationToken cancellationToken = default)
        {
            // Wrap the action callbacks in async delegates
            Func<string, Task> asyncMsgCallback = (text) => {
                onMessageReceived(text);
                return Task.CompletedTask;
            };

            Func<StreamingEventData, Task>? asyncEventCallback = null;
            if (onEventReceived != null) {
                asyncEventCallback = (eventData) => {
                    onEventReceived(eventData);
                    return Task.CompletedTask;
                };
            }

            // Use the async implementation
            return await SendMessageStreamWithEventsAsyncImplementation(
                agentId, message, threadId, 
                asyncMsgCallback, asyncEventCallback, 
                cancellationToken);
        }

        // Async callback implementation
        public async Task<string?> SendMessageStreamWithEventsAsync(
            string agentId, 
            string message, 
            string? threadId, 
            Func<string, Task> onMessageReceived, 
            Func<StreamingEventData, Task>? onEventReceived = null, 
            CancellationToken cancellationToken = default)
        {
            return await SendMessageStreamWithEventsAsyncImplementation(
                agentId, message, threadId, 
                onMessageReceived, onEventReceived, 
                cancellationToken);
        }

        // The actual implementation that both overloads use
        private async Task<string?> SendMessageStreamWithEventsAsyncImplementation(
            string agentId, 
            string message, 
            string? threadId, 
            Func<string, Task> onMessageReceived, 
            Func<StreamingEventData, Task>? onEventReceived = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new MessageRequest { Text = message, ThreadId = threadId };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/agents/{agentId}/messages/stream")
                {
                    Content = content
                };

                httpRequest.Headers.Add("Accept", "text/event-stream");
                httpRequest.Headers.Add("Cache-Control", "no-cache");
                httpRequest.Headers.Add("Connection", "keep-alive");

                System.Diagnostics.Debug.WriteLine("Starting enhanced streaming request...");
                
                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? resultThreadId = null;
                string? currentEvent = null;
                var chunksReceived = 0;
                var jsonBuffer = new StringBuilder(); // Buffer for multi-line JSON
                
                System.Diagnostics.Debug.WriteLine("Starting to read enhanced streaming response...");
                
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync(cancellationToken);
                        if (line == null) 
                        {
                            System.Diagnostics.Debug.WriteLine("Received null line, stream may be ending");
                            break;
                        }

                        System.Diagnostics.Debug.WriteLine($"Received line: '{line}'");

                        // Handle Server-Sent Events format
                        if (line.StartsWith("event: "))
                        {
                            currentEvent = line.Substring(7).Trim();
                            System.Diagnostics.Debug.WriteLine($"Event type: {currentEvent}");
                            
                            // Reset JSON buffer for new event
                            jsonBuffer.Clear();
                        }
                        else if (line.StartsWith("data: "))
                        {
                            var data = line.Substring(6);
                            System.Diagnostics.Debug.WriteLine($"Data received for event '{currentEvent}': '{data}'");
                            
                            // Handle different event types based on the new API
                            switch (currentEvent)
                            {
                                case "connected":
                                    if (onEventReceived != null)
                                    {
                                        try
                                        {
                                            await onEventReceived(new StreamingEventData { EventType = "connected" });
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[STREAM] Error in connected event callback: {ex.Message}");
                                        }
                                    }
                                    break;

                                case "created":
                                    try
                                    {
                                        var createdData = JsonSerializer.Deserialize<StreamingEventData>(data, _jsonOptions);
                                        if (createdData != null && onEventReceived != null)
                                        {
                                            createdData.EventType = "created";
                                            try
                                            {
                                                await onEventReceived(createdData);
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[STREAM] Error in created event callback: {ex.Message}");
                                            }
                                        }
                                    }
                                    catch (JsonException ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error parsing created event: {ex.Message}");
                                    }
                                    break;

                                case "delta":
                                    try
                                    {
                                        // Accumulate JSON data (might be split across multiple lines)
                                        jsonBuffer.Append(data);
                                        
                                        // Check if this line ends the JSON object
                                        if (data.Trim().EndsWith("}"))
                                        {
                                            var completeJson = jsonBuffer.ToString();
                                            var deltaData = JsonSerializer.Deserialize<StreamingEventData>(completeJson, _jsonOptions);
                                            if (deltaData != null && !string.IsNullOrEmpty(deltaData.Text))
                                            {
                                                chunksReceived++;
                                                System.Diagnostics.Debug.WriteLine($"[STREAM] Chunk #{chunksReceived}: '{deltaData.Text}' (length: {deltaData.Text.Length})");
                                                
                                                // Call the async message callback
                                                try 
                                                {
                                                    await onMessageReceived(deltaData.Text);
                                                }
                                                catch (Exception callbackEx)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"[STREAM] Error in message callback: {callbackEx.Message}");
                                                }

                                                if (onEventReceived != null)
                                                {
                                                    deltaData.EventType = "delta";
                                                    try 
                                                    {
                                                        await onEventReceived(deltaData);
                                                    }
                                                    catch (Exception eventCallbackEx)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[STREAM] Error in event callback: {eventCallbackEx.Message}");
                                                    }
                                                }
                                            }
                                            jsonBuffer.Clear();
                                        }
                                    }
                                    catch (JsonException ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error parsing delta event: {ex.Message}");
                                        jsonBuffer.Clear();
                                    }
                                    break;

                                case "stream_complete":
                                    try
                                    {
                                        var completeData = JsonSerializer.Deserialize<StreamingEventData>(data, _jsonOptions);
                                        if (completeData != null && onEventReceived != null)
                                        {
                                            completeData.EventType = "stream_complete";
                                            try
                                            {
                                                await onEventReceived(completeData);
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[STREAM] Error in stream_complete event callback: {ex.Message}");
                                            }
                                        }
                                    }
                                    catch (JsonException ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error parsing stream_complete event: {ex.Message}");
                                    }
                                    break;

                                case "complete":
                                    // Accumulate JSON data (might be split across multiple lines)
                                    jsonBuffer.Append(data);
                                    
                                    // Check if this line ends the JSON object
                                    if (data.Trim().EndsWith("}"))
                                    {
                                        // Complete JSON received, try to parse it
                                        var completeJson = jsonBuffer.ToString();
                                        System.Diagnostics.Debug.WriteLine($"Complete JSON accumulated: {completeJson}");
                                        
                                        try
                                        {
                                            var completeResponse = JsonSerializer.Deserialize<MessageResponse>(completeJson, _jsonOptions);
                                            if (completeResponse != null)
                                            {
                                                resultThreadId = completeResponse.ThreadId;
                                                System.Diagnostics.Debug.WriteLine($"Final response complete. ThreadId: {completeResponse.ThreadId}");
                                                
                                                // If there's text in the complete response and we didn't get streaming chunks,
                                                // send the complete text as a single chunk
                                                if (chunksReceived == 0 && !string.IsNullOrEmpty(completeResponse.Text))
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"No streaming chunks received, sending complete text: '{completeResponse.Text}'");
                                                    try
                                                    {
                                                        await onMessageReceived(completeResponse.Text);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[STREAM] Error sending complete text: {ex.Message}");
                                                    }
                                                    chunksReceived = 1;
                                                }

                                                if (onEventReceived != null)
                                                {
                                                    try
                                                    {
                                                        await onEventReceived(new StreamingEventData 
                                                        { 
                                                            EventType = "complete", 
                                                            ThreadId = completeResponse.ThreadId,
                                                            Text = completeResponse.Text
                                                        });
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[STREAM] Error in complete event callback: {ex.Message}");
                                                    }
                                                }
                                            }
                                        }
                                        catch (JsonException ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error parsing complete response: {ex.Message}");
                                            System.Diagnostics.Debug.WriteLine($"JSON that failed to parse: {completeJson}");
                                        }
                                        
                                        jsonBuffer.Clear();
                                    }
                                    break;

                                case "stream_error":
                                case "error":
                                    try
                                    {
                                        jsonBuffer.Append(data);
                                        if (data.Trim().EndsWith("}"))
                                        {
                                            var completeJson = jsonBuffer.ToString();
                                            var errorData = JsonSerializer.Deserialize<StreamingEventData>(completeJson, _jsonOptions);
                                            if (errorData != null)
                                            {
                                                errorData.EventType = currentEvent;
                                                if (onEventReceived != null)
                                                {
                                                    try
                                                    {
                                                        await onEventReceived(errorData);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[STREAM] Error in error event callback: {ex.Message}");
                                                    }
                                                }

                                                try
                                                {
                                                    await onMessageReceived($"Error: {errorData.Error}");
                                                }
                                                catch (Exception ex)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"[STREAM] Error sending error message: {ex.Message}");
                                                }
                                            }
                                            jsonBuffer.Clear();
                                        }
                                    }
                                    catch (JsonException ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error parsing error event: {ex.Message}");
                                        jsonBuffer.Clear();
                                    }
                                    break;

                                case "reasoning":
                                    try
                                    {
                                        jsonBuffer.Append(data);
                                        if (data.Trim().EndsWith("}"))
                                        {
                                            var completeJson = jsonBuffer.ToString();
                                            var reasoningData = JsonSerializer.Deserialize<StreamingEventData>(completeJson, _jsonOptions);
                                            if (reasoningData != null)
                                            {
                                                reasoningData.EventType = "reasoning";
                                                if (onEventReceived != null)
                                                {
                                                    try
                                                    {
                                                        await onEventReceived(reasoningData);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[STREAM] Error in reasoning event callback: {ex.Message}");
                                                    }
                                                }
                                                
                                                // Optionally add reasoning text to the main stream
                                                if (!string.IsNullOrEmpty(reasoningData.Text))
                                                {
                                                    try
                                                    {
                                                        await onMessageReceived($"[Reasoning] {reasoningData.Text}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[STREAM] Error sending reasoning text: {ex.Message}");
                                                    }
                                                }
                                            }
                                            jsonBuffer.Clear();
                                        }
                                    }
                                    catch (JsonException ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error parsing reasoning event: {ex.Message}");
                                        jsonBuffer.Clear();
                                    }
                                    break;

                                default:
                                    System.Diagnostics.Debug.WriteLine($"Unhandled event type: {currentEvent}");
                                    if (onEventReceived != null)
                                    {
                                        try
                                        {
                                            await onEventReceived(new StreamingEventData { EventType = currentEvent ?? "unknown" });
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[STREAM] Error in unknown event callback: {ex.Message}");
                                        }
                                    }
                                    break;
                            }
                        }
                        else if (string.IsNullOrEmpty(line))
                        {
                            // Empty line resets the event
                            if (currentEvent == "complete" && jsonBuffer.Length > 0)
                            {
                                // Try to parse accumulated JSON even if we didn't see a closing brace
                                var completeJson = jsonBuffer.ToString();
                                System.Diagnostics.Debug.WriteLine($"End of event, trying to parse JSON: {completeJson}");
                                
                                try
                                {
                                    var completeResponse = JsonSerializer.Deserialize<MessageResponse>(completeJson, _jsonOptions);
                                    if (completeResponse != null)
                                    {
                                        resultThreadId = completeResponse.ThreadId;
                                        System.Diagnostics.Debug.WriteLine($"Response complete. ThreadId: {completeResponse.ThreadId}");
                                        
                                        if (chunksReceived == 0 && !string.IsNullOrEmpty(completeResponse.Text))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Sending complete text: '{completeResponse.Text}'");
                                            try
                                            {
                                                await onMessageReceived(completeResponse.Text);
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[STREAM] Error sending complete text at event end: {ex.Message}");
                                            }
                                            chunksReceived = 1;
                                        }

                                        if (onEventReceived != null)
                                        {
                                            try
                                            {
                                                await onEventReceived(new StreamingEventData 
                                                { 
                                                    EventType = "complete", 
                                                    ThreadId = completeResponse.ThreadId,
                                                    Text = completeResponse.Text
                                                });
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[STREAM] Error in complete event callback at event end: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error parsing complete response at event end: {ex.Message}");
                                }
                            }
                            
                            currentEvent = null;
                            jsonBuffer.Clear();
                        }
                        else if (line.StartsWith(":"))
                        {
                            // This is a comment line (heartbeat), ignore it
                            System.Diagnostics.Debug.WriteLine("Received heartbeat comment");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Unhandled line: '{line}'");
                        }
                    }
                    catch (Exception lineEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing line: {lineEx.Message}");
                        // Continue processing other lines
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Enhanced streaming ended. Total chunks received: {chunksReceived}");
                return resultThreadId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in enhanced streaming: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                try
                {
                    await onMessageReceived($"Error: {ex.Message}");
                }
                catch (Exception callbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[STREAM] Error sending error message: {callbackEx.Message}");
                }
                return null;
            }
        }

        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}