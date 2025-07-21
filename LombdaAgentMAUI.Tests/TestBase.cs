using Microsoft.Extensions.DependencyInjection;
using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Tests.Mocks;

namespace LombdaAgentMAUI.Tests
{
    /// <summary>
    /// Base class for tests that provides common setup and utilities
    /// </summary>
    [TestFixture]
    public abstract class TestBase
    {
        protected ServiceProvider? ServiceProvider;
        protected IConfigurationService? ConfigurationService;
        protected IAgentApiService? ApiService;
        protected MockSecureStorageService? MockSecureStorage;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // Override in derived classes for specific setup
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            ServiceProvider?.Dispose();
        }

        [SetUp]
        public virtual void SetUp()
        {
            // Common setup for all tests
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            ConfigurationService = ServiceProvider.GetService<IConfigurationService>();
            ApiService = ServiceProvider.GetService<IAgentApiService>();
            MockSecureStorage = ServiceProvider.GetService<ISecureStorageService>() as MockSecureStorageService;
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up after each test
            CleanupStorage();
            ServiceProvider?.Dispose();
            ServiceProvider = null;
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Default service configuration with mock storage
            services.AddSingleton<ISecureStorageService, MockSecureStorageService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            services.AddTransient<HttpClient>(provider =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri("http://localhost:5000/")
                };
                return httpClient;
            });
            
            services.AddTransient<IAgentApiService, AgentApiService>();
        }

        protected virtual void CleanupStorage()
        {
            try
            {
                // Clean up mock storage after tests
                MockSecureStorage?.Clear();
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        protected static void AssertNotNullOrEmpty(string? value, string parameterName = "value")
        {
            Assert.That(value, Is.Not.Null, $"{parameterName} should not be null");
            Assert.That(value, Is.Not.Empty, $"{parameterName} should not be empty");
        }

        protected static void AssertValidGuid(string? value, string parameterName = "value")
        {
            AssertNotNullOrEmpty(value, parameterName);
            Assert.That(Guid.TryParse(value, out _), Is.True, $"{parameterName} should be a valid GUID");
        }

        protected static void AssertValidUrl(string? value, string parameterName = "value")
        {
            AssertNotNullOrEmpty(value, parameterName);
            Assert.That(Uri.TryCreate(value, UriKind.Absolute, out _), Is.True, $"{parameterName} should be a valid URL");
        }

        protected static void AssertValidTimestamp(DateTime timestamp, string parameterName = "timestamp")
        {
            Assert.That(timestamp, Is.GreaterThan(DateTime.MinValue), $"{parameterName} should be a valid timestamp");
            Assert.That(timestamp, Is.LessThanOrEqualTo(DateTime.Now.AddMinutes(1)), $"{parameterName} should not be in the future");
        }
    }

    /// <summary>
    /// Attribute to mark tests that require network access
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequiresNetworkAttribute : Attribute
    {
        public string Reason { get; }

        public RequiresNetworkAttribute(string reason = "This test requires network access")
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Attribute to mark tests that are slow running
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SlowTestAttribute : Attribute
    {
        public string Reason { get; }

        public SlowTestAttribute(string reason = "This test may take longer to execute")
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Test categories for organizing tests
    /// </summary>
    public static class TestCategories
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string Api = "Api";
        public const string Configuration = "Configuration";
        public const string Models = "Models";
        public const string Converters = "Converters";
        public const string Services = "Services";
        public const string Network = "Network";
        public const string Slow = "Slow";
    }
}