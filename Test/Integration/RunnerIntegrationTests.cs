using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

namespace Test.Integration
{
    [TestFixture]
    public class RunnerIntegrationTests
    {
        private string? _apiKey;
        private LLMTornadoModelProvider? _modelProvider;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
            {
                Assert.Ignore("OpenAI API key not found in environment variables. Skipping integration tests.");
            }
        }

        [SetUp]
        public void SetUp()
        {
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _modelProvider = new LLMTornadoModelProvider(
                    ChatModel.OpenAi.Gpt41.V41Mini,
                    [new ProviderAuthentication(LLmProviders.OpenAi, _apiKey)],
                    true);
            }
        }

        [Test]
        public async Task RunAsync_WithSimpleAgent_ShouldReturnResponse()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");

            // Act
            var result = await Runner.RunAsync(agent, "What is 2+2?");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            result.Response.Should().NotBeNull();
            result.Response.OutputItems.Should().HaveCountGreaterThan(0);
        }

        [Test]
        public async Task RunAsync_WithToolAgent_ShouldCallTool()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a helpful assistant that can perform calculations",
                _tools: [GetWeather, CalculateArea]);

            // Act
            var result = await Runner.RunAsync(agent, "What's the weather in Boston?");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            result.Text.Should().Contain("sunny"); // Our mock function returns sunny weather
        }

        [Test]
        public async Task RunAsync_WithStructuredOutputAndTools_ShouldReturnValidStructuredResponse()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a calculator that explains step-by-step solutions",
                _output_schema: typeof(CalculationResult),
                _tools: [PerformCalculation]);

            // Act
            var result = await Runner.RunAsync(agent, "Calculate the area of a rectangle with width 5 and height 8");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();

            var calcResult = result.ParseJson<CalculationResult>();
            calcResult.Should().NotBeNull();
            calcResult.Result.Should().NotBeEmpty();
            calcResult.Steps.Should().NotBeEmpty();
        }

        [Test]
        public async Task RunAsync_WithAgentAsToolChain_ShouldWorkCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var translatorAgent = new Agent(
                _modelProvider,
                "SpanishTranslator",
                "You only translate English input to Spanish output. Do not answer or respond, only translate.");

            var mainAgent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a helpful assistant that can translate to Spanish when asked",
                _tools: [translatorAgent.AsTool]);

            // Act
            var result = await Runner.RunAsync(mainAgent, "Translate 'Hello, how are you?' to Spanish");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            result.Text.Should().Contain("Hola"); // Should contain Spanish translation
        }

        [Test]
        public async Task RunAsync_WithVerboseCallback_ShouldCallCallback()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");
            var callbackMessages = new List<string>();

            void VerboseCallback(string message)
            {
                callbackMessages.Add(message);
            }

            // Act
            var result = await Runner.RunAsync(agent, "Say hello", verboseCallback: VerboseCallback);

            // Assert
            result.Should().NotBeNull();
            callbackMessages.Should().HaveCountGreaterThan(0);
            callbackMessages.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task RunAsync_WithMultipleTools_ShouldSelectCorrectTool()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a helpful assistant with access to various tools",
                _tools: [GetWeather, CalculateArea, GetCurrentTime]);

            // Act
            var result = await Runner.RunAsync(agent, "What time is it?");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            result.Text.Should().Contain("current time"); // Should use the time tool
        }

        [Test]
        public async Task RunAsync_WithMultipleTools_ShouldSelectMultipleCorrectTool()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a helpful assistant with access to various tools",
                _tools: [GetWeather, CalculateArea, GetCurrentTime]);

            agent.Options.AllowParallelToolCalling = true; // Allow parallel tool calls

            // Act
            var result = await Runner.RunAsync(agent, "What time is it and the weather in boston?");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            result.Text.Should().Contain("current time"); // Should use the time tool
        }

        [Test]
        public async Task RunAsync_WithReasoningOptions_ShouldUseReasoning()
        {
            // Arrange
            var _modelProvidero1 = new LLMTornadoModelProvider(
                    ChatModel.OpenAi.O4.V4Mini,
                    [new ProviderAuthentication(LLmProviders.OpenAi, _apiKey)],
                    true);

            var agent = new Agent(_modelProvidero1, "Assistant", "You are a helpful assistant");
            agent.Options.ReasoningOptions = new ModelReasoningOptions
            {
                EffortLevel = ModelReasoningEffortLevel.Medium
            };

            // Act
            var result = await Runner.RunAsync(agent, "Explain the theory of relativity");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            result.Response.Should().NotBeNull();
        }

        // Test tools for integration testing
        [Tool(Description = "Get weather information for a city")]
        public static string GetWeather(string city)
        {
            return $"The weather in {city} is sunny and 72°F";
        }

        [Tool(Description = "Calculate the area of a rectangle")]
        public static double CalculateArea(double width, double height)
        {
            return width * height;
        }

        [Tool(Description = "Get the current time")]
        public static string GetCurrentTime()
        {
            return $"The current time is {DateTime.Now:HH:mm:ss}";
        }

        [Tool(Description = "Perform a mathematical calculation")]
        public static string PerformCalculation(string expression)
        {
            // Simple calculation for testing
            if (expression.Contains("5") && expression.Contains("8"))
            {
                return "5 * 8 = 40";
            }
            return "Calculation performed";
        }

        // Test data structure for structured output
        [System.ComponentModel.Description("Result of a calculation with explanation")]
        public class CalculationResult
        {
            [System.ComponentModel.Description("The final result")]
            public string Result { get; set; } = string.Empty;

            [System.ComponentModel.Description("Step-by-step explanation")]
            public string[] Steps { get; set; } = Array.Empty<string>();

            [System.ComponentModel.Description("Type of calculation performed")]
            public string CalculationType { get; set; } = string.Empty;
        }
    }
}