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
                char[] charBuffer = new char[1];

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await reader.ReadAsync(charBuffer, 0, 1);
                    if (bytesRead > 0)
                    {
                        char currentChar = charBuffer[0];
                        buffer.Append(currentChar);

                        if (currentChar == '\n')
                        {
                            var line = buffer.ToString().TrimEnd('\n', '\r');
                            buffer.Clear();

                            if (line.StartsWith("data: ") && !line.StartsWith("data: {"))
                            {
                                var messageData = line.Substring(6);
                                if (!string.IsNullOrWhiteSpace(messageData))
                                {
                                    onMessageReceived(messageData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in streaming: {ex.Message}");
                onMessageReceived($"Error: {ex.Message}");
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