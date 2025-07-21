using System.Text;
using System.Text.Json;
using LombdaAgentMAUI.Core.Models;

namespace LombdaAgentMAUI.Core.Services
{
    public interface IAgentApiService
    {
        Task<List<string>> GetAgentsAsync();
        Task<AgentResponse?> CreateAgentAsync(string name);
        Task<AgentResponse?> GetAgentAsync(string id);
        Task<MessageResponse?> SendMessageAsync(string agentId, string message, string? threadId = null);
        Task SendMessageStreamAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default);
        Task<string?> SendMessageStreamWithThreadAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default);
        
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

        public async Task<AgentResponse?> CreateAgentAsync(string name)
        {
            try
            {
                var request = new AgentCreationRequest { Name = name };
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

        public async Task SendMessageStreamAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
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

                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var buffer = new StringBuilder();
                string? currentEvent = null;
                
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    // Handle Server-Sent Events format
                    if (line.StartsWith("event: "))
                    {
                        currentEvent = line.Substring(7).Trim();
                    }
                    else if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        
                        if (currentEvent == "message")
                        {
                            // This is a streaming message chunk
                            if (!string.IsNullOrWhiteSpace(data))
                            {
                                onMessageReceived(data);
                            }
                        }
                        else if (currentEvent == "complete")
                        {
                            // This is the final complete response with thread ID
                            try
                            {
                                var completeResponse = JsonSerializer.Deserialize<MessageResponse>(data, _jsonOptions);
                                if (completeResponse != null)
                                {
                                    // Store the thread ID for future messages (if needed)
                                    // Note: In MAUI, we'll handle this in the UI layer
                                    System.Diagnostics.Debug.WriteLine($"Streaming complete. ThreadId: {completeResponse.ThreadId}");
                                }
                            }
                            catch (JsonException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error parsing complete response: {ex.Message}");
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(line))
                    {
                        // Empty line resets the event
                        currentEvent = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in streaming: {ex.Message}");
                onMessageReceived($"Error: {ex.Message}");
            }
        }

        public async Task<string?> SendMessageStreamWithThreadAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
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

                System.Diagnostics.Debug.WriteLine("Starting streaming request...");
                
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
                
                System.Diagnostics.Debug.WriteLine("Starting to read streaming response...");
                
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync();
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
                            
                            if (currentEvent == "message")
                            {
                                // This is a streaming message chunk
                                if (!string.IsNullOrWhiteSpace(data))
                                {
                                    chunksReceived++;
                                    System.Diagnostics.Debug.WriteLine($"Calling onMessageReceived with chunk #{chunksReceived}: '{data}'");
                                    onMessageReceived(data);
                                }
                            }
                            else if (currentEvent == "complete")
                            {
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
                                            System.Diagnostics.Debug.WriteLine($"Streaming complete. ThreadId: {completeResponse.ThreadId}");
                                            
                                            // If there's text in the complete response and we didn't get streaming chunks,
                                            // send the complete text as a single chunk
                                            if (chunksReceived == 0 && !string.IsNullOrEmpty(completeResponse.Text))
                                            {
                                                System.Diagnostics.Debug.WriteLine($"No streaming chunks received, sending complete text: '{completeResponse.Text}'");
                                                onMessageReceived(completeResponse.Text);
                                                chunksReceived = 1;
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
                                        System.Diagnostics.Debug.WriteLine($"Streaming complete. ThreadId: {completeResponse.ThreadId}");
                                        
                                        if (chunksReceived == 0 && !string.IsNullOrEmpty(completeResponse.Text))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Sending complete text: '{completeResponse.Text}'");
                                            onMessageReceived(completeResponse.Text);
                                            chunksReceived = 1;
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
                
                System.Diagnostics.Debug.WriteLine($"Streaming ended. Total chunks received: {chunksReceived}");
                return resultThreadId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in streaming: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                onMessageReceived($"Error: {ex.Message}");
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