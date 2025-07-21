using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;

namespace LombdaAgentMAUI.Services
{
    /// <summary>
    /// MAUI-specific wrapper for AgentApiService that handles dynamic URL configuration
    /// </summary>
    public class MauiAgentApiService : IAgentApiService, IDisposable
    {
        private readonly IConfigurationService _configService;
        private AgentApiService? _currentApiService;
        private string? _lastUsedUrl;

        public MauiAgentApiService(IConfigurationService configService)
        {
            _configService = configService;
        }

        private AgentApiService GetOrCreateApiService()
        {
            var currentUrl = _configService.ApiBaseUrl;
            
            // Create new service if URL changed or doesn't exist
            if (_currentApiService == null || _lastUsedUrl != currentUrl)
            {
                _currentApiService?.Dispose();
                
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(currentUrl),
                    Timeout = TimeSpan.FromMinutes(5)
                };
                
                _currentApiService = new AgentApiService(httpClient);
                _lastUsedUrl = currentUrl;
            }

            return _currentApiService;
        }

        public void UpdateBaseUrl(string baseUrl)
        {
            // Update configuration first
            _configService.ApiBaseUrl = baseUrl;
            
            // Force creation of new service on next call
            _currentApiService?.Dispose();
            _currentApiService = null;
            _lastUsedUrl = null;
        }

        public async Task<List<string>> GetAgentsAsync()
        {
            return await GetOrCreateApiService().GetAgentsAsync();
        }

        public async Task<AgentResponse?> CreateAgentAsync(string name)
        {
            return await GetOrCreateApiService().CreateAgentAsync(name);
        }

        public async Task<AgentResponse?> GetAgentAsync(string id)
        {
            return await GetOrCreateApiService().GetAgentAsync(id);
        }

        public async Task<MessageResponse?> SendMessageAsync(string agentId, string message, string? threadId = null)
        {
            return await GetOrCreateApiService().SendMessageAsync(agentId, message, threadId);
        }

        public async Task SendMessageStreamAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
        {
            await GetOrCreateApiService().SendMessageStreamAsync(agentId, message, threadId, onMessageReceived, cancellationToken);
        }

        public async Task<string?> SendMessageStreamWithThreadAsync(string agentId, string message, string? threadId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
        {
            return await GetOrCreateApiService().SendMessageStreamWithThreadAsync(agentId, message, threadId, onMessageReceived, cancellationToken);
        }

        public void Dispose()
        {
            _currentApiService?.Dispose();
        }
    }
}