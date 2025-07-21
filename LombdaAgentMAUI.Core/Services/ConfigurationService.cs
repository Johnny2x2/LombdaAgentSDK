namespace LombdaAgentMAUI.Core.Services
{
    public interface IConfigurationService
    {
        string ApiBaseUrl { get; set; }
        Task SaveSettingsAsync();
        Task LoadSettingsAsync();
    }

    /// <summary>
    /// Configuration service for core library - uses abstract storage
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private const string DEFAULT_API_URL = "https://localhost:5001/";
        private readonly ISecureStorageService _secureStorageService;

        public string ApiBaseUrl { get; set; } = DEFAULT_API_URL;

        public ConfigurationService(ISecureStorageService secureStorageService)
        {
            _secureStorageService = secureStorageService;
        }

        public async Task SaveSettingsAsync()
        {
            await _secureStorageService.SetAsync("api_base_url", ApiBaseUrl);
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                var savedUrl = await _secureStorageService.GetAsync("api_base_url");
                if (!string.IsNullOrWhiteSpace(savedUrl))
                {
                    ApiBaseUrl = savedUrl;
                }
            }
            catch (Exception)
            {
                // If there's an issue reading from secure storage, use default
                ApiBaseUrl = DEFAULT_API_URL;
            }
        }
    }

    /// <summary>
    /// Abstract interface for secure storage to allow platform-specific implementations
    /// </summary>
    public interface ISecureStorageService
    {
        Task<string?> GetAsync(string key);
        Task SetAsync(string key, string value);
        void Remove(string key);
    }
}