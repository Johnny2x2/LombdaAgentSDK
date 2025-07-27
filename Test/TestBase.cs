using Microsoft.Extensions.Configuration;

namespace Test
{
    [SetUpFixture]
    public class TestSetup
    {
        public static IConfiguration? Configuration { get; private set; }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Setup configuration
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.test.json", optional: true);

            Configuration = builder.Build();

            // Ensure test environment is properly configured
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Warning: OPENAI_API_KEY environment variable not set. Integration tests will be skipped.");
            }
            else
            {
                Console.WriteLine("OpenAI API key found. Integration tests will run.");
            }
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            // Cleanup if needed
        }
    }

    /// <summary>
    /// Base class for all tests providing common functionality
    /// </summary>
    public abstract class TestBase
    {
        protected string? ApiKey { get; private set; }
        protected bool CanRunIntegrationTests => !string.IsNullOrEmpty(ApiKey);

        [SetUp]
        public virtual void SetUp()
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Cleanup after each test
        }

        protected void SkipIfNoApiKey()
        {
            if (!CanRunIntegrationTests)
            {
                Assert.Ignore("OpenAI API key not available. Skipping integration test.");
            }
        }

        protected static void AssertValidJsonString(string json)
        {
            json.Should().NotBeNullOrWhiteSpace();
            var exception = Record.Exception(() => System.Text.Json.JsonDocument.Parse(json));
            exception.Should().BeNull("JSON should be valid");
        }

        protected static void AssertContainsAnyOf(string text, params string[] expectedSubstrings)
        {
            text.Should().NotBeNullOrWhiteSpace();
            expectedSubstrings.Should().NotBeNullOrEmpty();
            
            var containsAny = expectedSubstrings.Any(substring => 
                text.Contains(substring, StringComparison.OrdinalIgnoreCase));
            
            containsAny.Should().BeTrue($"Text should contain at least one of: {string.Join(", ", expectedSubstrings)}");
        }
    }
}