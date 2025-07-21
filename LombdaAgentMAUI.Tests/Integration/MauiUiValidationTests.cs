using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LombdaAgentMAUI.Tests.Integration
{
    /// <summary>
    /// Quick validation tests to ensure the MAUI UI fixes work correctly
    /// </summary>
    [TestFixture]
    [Category(TestCategories.Integration)]
    public class MauiUiValidationTests
    {
        private ServiceProvider _serviceProvider;
        private IAgentApiService _apiService;
        private IConfigurationService _configService;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Use a test configuration service that doesn't persist to real storage
            services.AddSingleton<ISecureStorageService>(provider => new TestSecureStorageService());
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Use real HTTP client for testing
            services.AddHttpClient<IAgentApiService, AgentApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001/");
                client.Timeout = TimeSpan.FromMinutes(2);
            });
            
            _serviceProvider = services.BuildServiceProvider();
            _apiService = _serviceProvider.GetRequiredService<IAgentApiService>();
            _configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }

        [Test]
        public void ConfigurationService_CanBeCreated()
        {
            // Assert
            Assert.That(_configService, Is.Not.Null);
            Assert.That(_configService.ApiBaseUrl, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void AgentApiService_CanBeCreated()
        {
            // Assert
            Assert.That(_apiService, Is.Not.Null);
        }

        [Test]
        public void AgentApiService_CanUpdateBaseUrl()
        {
            // Arrange
            var newUrl = "https://test.example.com/";

            // Act & Assert (should not throw)
            Assert.DoesNotThrow(() => _apiService.UpdateBaseUrl(newUrl));
        }

        [Test]
        public async Task ConfigurationService_SaveAndLoad_WorksCorrectly()
        {
            // Arrange
            var testUrl = "https://ui-test.example.com/";
            _configService.ApiBaseUrl = testUrl;

            // Act
            await _configService.SaveSettingsAsync();
            
            // Create new instance to simulate app restart
            var newConfigService = new ConfigurationService(_serviceProvider.GetRequiredService<ISecureStorageService>());
            await newConfigService.LoadSettingsAsync();

            // Assert
            Assert.That(newConfigService.ApiBaseUrl, Is.EqualTo(testUrl));
        }

        [Test]
        [Category(TestCategories.Network)]
        [RequiresNetwork("Test requires running API server")]
        public async Task Integration_ConfigAndApiService_WorkTogether()
        {
            // This test verifies that the configuration and API service work together
            // as they would in the MAUI UI
            
            // Arrange
            _configService.ApiBaseUrl = "https://localhost:5001/";
            _apiService.UpdateBaseUrl(_configService.ApiBaseUrl);

            // Act
            var agents = await _apiService.GetAgentsAsync();

            // Assert
            Assert.That(agents, Is.Not.Null);
            Console.WriteLine($"? Found {agents.Count} agents through updated API service");
        }

        [Test]
        public async Task ServiceConfiguration_HandlesDifferentUrls()
        {
            // Arrange
            var urls = new[]
            {
                "https://localhost:5001/",
                "http://localhost:5000/",
                "https://test.example.com/"
            };

            foreach (var url in urls)
            {
                // Act
                _configService.ApiBaseUrl = url;
                await _configService.SaveSettingsAsync();
                _apiService.UpdateBaseUrl(_configService.ApiBaseUrl);

                // Assert
                Assert.That(_configService.ApiBaseUrl, Is.EqualTo(url));
                Console.WriteLine($"? Successfully configured for URL: {url}");
            }
        }

        /// <summary>
        /// Simple in-memory storage for testing
        /// </summary>
        private class TestSecureStorageService : ISecureStorageService
        {
            private readonly Dictionary<string, string> _storage = new();

            public Task<string?> GetAsync(string key)
            {
                _storage.TryGetValue(key, out var value);
                return Task.FromResult(value);
            }

            public Task SetAsync(string key, string value)
            {
                _storage[key] = value;
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                _storage.Remove(key);
            }
        }
    }
}