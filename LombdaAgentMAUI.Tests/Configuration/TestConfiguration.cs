using System.Text.Json;

namespace LombdaAgentMAUI.Tests.Configuration
{
    /// <summary>
    /// Configuration helper for running tests against different environments
    /// </summary>
    public static class TestConfiguration
    {
        public static class Environment
        {
            public const string Local = "Local";
            public const string Development = "Development";
            public const string Staging = "Staging";
            public const string Production = "Production";
        }

        public static class Endpoints
        {
            public const string LocalHttps = "https://localhost:5001/";
            public const string LocalHttp = "http://localhost:5000/";
            public const string LocalDockerHttps = "https://localhost:7001/";
            public const string LocalDockerHttp = "http://localhost:7000/";
        }

        /// <summary>
        /// Get the API URL to use for testing based on environment variables or defaults
        /// </summary>
        public static string GetApiUrl()
        {
            // Check environment variable first
            var envUrl = System.Environment.GetEnvironmentVariable("LOMBDA_TEST_API_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                return envUrl;
            }

            // Check for test environment setting
            var testEnv = GetTestEnvironment();
            return testEnv switch
            {
                Environment.Local => Endpoints.LocalHttps,
                Environment.Development => Endpoints.LocalDockerHttps,
                Environment.Staging => "https://staging-api.lombda.com/", // Example staging URL
                Environment.Production => "https://api.lombda.com/", // Example production URL
                _ => Endpoints.LocalHttps
            };
        }

        /// <summary>
        /// Get the test environment from environment variable or default to Local
        /// </summary>
        public static string GetTestEnvironment()
        {
            return System.Environment.GetEnvironmentVariable("LOMBDA_TEST_ENVIRONMENT") ?? Environment.Local;
        }

        /// <summary>
        /// Get timeout duration for API calls based on environment
        /// </summary>
        public static TimeSpan GetApiTimeout()
        {
            var testEnv = GetTestEnvironment();
            return testEnv switch
            {
                Environment.Local => TimeSpan.FromMinutes(2),
                Environment.Development => TimeSpan.FromMinutes(3),
                Environment.Staging => TimeSpan.FromMinutes(5),
                Environment.Production => TimeSpan.FromMinutes(5),
                _ => TimeSpan.FromMinutes(2)
            };
        }

        /// <summary>
        /// Check if live server tests should be run
        /// </summary>
        public static bool ShouldRunLiveTests()
        {
            var runLiveTests = System.Environment.GetEnvironmentVariable("LOMBDA_RUN_LIVE_TESTS");
            return string.Equals(runLiveTests, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(runLiveTests, "1", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get OpenAI API key for tests that require it
        /// </summary>
        public static string? GetOpenAIApiKey()
        {
            return System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        /// <summary>
        /// Check if the specified API endpoint is accessible
        /// </summary>
        public static async Task<bool> IsApiAccessibleAsync(string apiUrl, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(10);

            try
            {
                using var httpClient = new HttpClient { Timeout = timeout };
                var response = await httpClient.GetAsync($"{apiUrl.TrimEnd('/')}/v1/agents");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Print current test configuration
        /// </summary>
        public static void PrintConfiguration()
        {
            Console.WriteLine("?? Test Configuration:");
            Console.WriteLine($"   Environment: {GetTestEnvironment()}");
            Console.WriteLine($"   API URL: {GetApiUrl()}");
            Console.WriteLine($"   API Timeout: {GetApiTimeout()}");
            Console.WriteLine($"   Run Live Tests: {ShouldRunLiveTests()}");
            Console.WriteLine($"   OpenAI Key Set: {!string.IsNullOrEmpty(GetOpenAIApiKey())}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Test data for different scenarios
    /// </summary>
    public static class TestScenarios
    {
        public static class Messages
        {
            public const string SimpleGreeting = "Hello! Please respond with a simple greeting.";
            public const string MemoryTest = "My name is TestUser. Please remember this.";
            public const string MemoryQuery = "What is my name?";
            public const string CountingRequest = "Please count from 1 to 5, saying each number on a new line.";
            public const string ComplexQuestion = "Can you explain what you are and what you can help me with?";
            public const string MathQuestion = "What is 15 + 27?";
            public const string CreativeTask = "Write a short haiku about testing software.";
        }

        public static class AgentNames
        {
            public static string GenerateTestAgentName(string prefix = "TestAgent")
            {
                return $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
            }

            public static string GenerateUniqueName(string baseName)
            {
                return $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        public static class Timeouts
        {
            public static readonly TimeSpan Quick = TimeSpan.FromSeconds(30);
            public static readonly TimeSpan Standard = TimeSpan.FromMinutes(2);
            public static readonly TimeSpan Long = TimeSpan.FromMinutes(5);
            public static readonly TimeSpan VeryLong = TimeSpan.FromMinutes(10);
        }
    }

    /// <summary>
    /// Test results and reporting helpers
    /// </summary>
    public static class TestReporting
    {
        public static void LogTestStart(string testName, string description = "")
        {
            Console.WriteLine($"?? Starting: {testName}");
            if (!string.IsNullOrEmpty(description))
            {
                Console.WriteLine($"   ?? {description}");
            }
        }

        public static void LogTestSuccess(string testName, string details = "")
        {
            Console.WriteLine($"? Passed: {testName}");
            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine($"   ?? {details}");
            }
            Console.WriteLine();
        }

        public static void LogTestFailure(string testName, string error)
        {
            Console.WriteLine($"? Failed: {testName}");
            Console.WriteLine($"   ?? {error}");
            Console.WriteLine();
        }

        public static void LogApiCall(string method, string endpoint, string? requestData = null)
        {
            Console.WriteLine($"?? API Call: {method} {endpoint}");
            if (!string.IsNullOrEmpty(requestData))
            {
                Console.WriteLine($"   ?? Request: {requestData}");
            }
        }

        public static void LogApiResponse(string responseData, TimeSpan duration)
        {
            Console.WriteLine($"   ?? Response ({duration.TotalMilliseconds:F0}ms): {responseData}");
        }

        public static void LogStreamingUpdate(string chunk, int totalChunks)
        {
            Console.WriteLine($"   ?? Chunk {totalChunks}: {chunk}");
        }
    }
}