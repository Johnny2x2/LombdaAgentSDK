using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System.Diagnostics;

namespace Test.Performance
{
    [TestFixture]
    public class PerformanceTests : TestBase
    {
        private LLMTornadoModelProvider? _modelProvider;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            if (CanRunIntegrationTests)
            {
                _modelProvider = new LLMTornadoModelProvider(
                    ChatModel.OpenAi.Gpt41.V41Mini,
                    [new ProviderAuthentication(LLmProviders.OpenAi, ApiKey!)]);
            }
        }

        [Test]
        [Category("Performance")]
        public async Task RunAsync_SimpleAgent_ShouldCompleteWithinTimeout()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(30);

            // Act
            var result = await Runner.RunAsync(agent, "Say hello");
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            stopwatch.Elapsed.Should().BeLessThan(timeout, "Response should complete within timeout");
        }

        [Test]
        [Category("Performance")]
        public async Task RunAsync_WithTools_ShouldCompleteWithinTimeout()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant", _tools: [FastTool]);
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(45);

            // Act
            var result = await Runner.RunAsync(agent, "Use the fast tool with input 'test'");
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            stopwatch.Elapsed.Should().BeLessThan(timeout, "Tool call should complete within timeout");
        }

        [Test]
        [Category("Performance")]
        public async Task RunAsync_WithStructuredOutput_ShouldCompleteWithinTimeout()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You extract information", _output_schema: typeof(SimpleInfo));
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(45);

            // Act
            var result = await Runner.RunAsync(agent, "Extract info: Alice, 25, teacher");
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            stopwatch.Elapsed.Should().BeLessThan(timeout, "Structured output should complete within timeout");
            
            var parsed = result.ParseJson<SimpleInfo>();
            parsed.Should().NotBeNull();
        }

        [Test]
        [Category("Performance")]
        public async Task RunAsync_MultipleSequentialCalls_ShouldMaintainPerformance()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");
            var numberOfCalls = 3;
            var maxTimePerCall = TimeSpan.FromSeconds(30);
            var results = new List<(TimeSpan Duration, RunResult Result)>();

            // Act
            for (int i = 0; i < numberOfCalls; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await Runner.RunAsync(agent, $"Count to {i + 1}");
                stopwatch.Stop();
                
                results.Add((stopwatch.Elapsed, result));
            }

            // Assert
            results.Should().HaveCount(numberOfCalls);
            foreach (var (duration, result) in results)
            {
                result.Should().NotBeNull();
                result.Text.Should().NotBeEmpty();
                duration.Should().BeLessThan(maxTimePerCall, "Each call should complete within time limit");
            }

            // Performance shouldn't degrade significantly
            var firstCallTime = results[0].Duration;
            var lastCallTime = results[^1].Duration;
            var degradationRatio = lastCallTime.TotalMilliseconds / firstCallTime.TotalMilliseconds;
            degradationRatio.Should().BeLessThan(3.0, "Performance shouldn't degrade more than 3x");
        }

        [Test]
        [Category("Performance")]
        public async Task RunAsync_WithMultipleTools_ShouldSelectToolEfficiently()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant with many tools", 
                _tools: [Tool1, Tool2, Tool3, Tool4, Tool5]);
            
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(60);

            // Act
            var result = await Runner.RunAsync(agent, "Use tool3 with parameter 'test'");
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            stopwatch.Elapsed.Should().BeLessThan(timeout, "Tool selection should be efficient");
            result.Text.Should().Contain("Tool3", "Should have called the correct tool");
        }

        [Test]
        [Category("Performance")]
        public void Agent_Creation_WithManyTools_ShouldBeEfficient()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var tools = new List<Delegate> { Tool1, Tool2, Tool3, Tool4, Tool5, FastTool };
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant", _tools: tools);
            stopwatch.Stop();

            // Assert
            agent.Should().NotBeNull();
            agent.tool_list.Should().HaveCount(6);
            stopwatch.Elapsed.Should().BeLessThan(timeout, "Agent creation should be fast even with many tools");
        }

        [Test]
        [Category("Performance")]
        public async Task StreamingResponse_ShouldStartQuickly()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var streamingProvider = new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, ApiKey!)],
                useResponseAPI: false); // Use chat API for streaming

            var agent = new Agent(streamingProvider, "Assistant", "You are a helpful assistant");
            var firstTokenReceived = false;
            var stopwatch = Stopwatch.StartNew();
            var maxTimeToFirstToken = TimeSpan.FromSeconds(10);

            void StreamingCallback(ModelStreamingEvents evt)
            {
                if (evt is ModelStreamingOutputTextDeltaEvent && !firstTokenReceived)
                {
                    firstTokenReceived = true;
                    stopwatch.Stop();
                }
            }

            // Act
            var result = await Runner.RunAsync(agent, "Write a short poem", streamingCallback: StreamingCallback);

            // Assert
            result.Should().NotBeNull();
            firstTokenReceived.Should().BeTrue("Should have received at least one token");
            stopwatch.Elapsed.Should().BeLessThan(maxTimeToFirstToken, "First token should arrive quickly");
        }

        // Performance test tools
        [Tool(Description = "A fast tool for performance testing")]
        public static string FastTool(string input)
        {
            return $"Fast result: {input}";
        }

        [Tool(Description = "Tool 1")]
        public static string Tool1(string input) => $"Tool1: {input}";

        [Tool(Description = "Tool 2")]
        public static string Tool2(string input) => $"Tool2: {input}";

        [Tool(Description = "Tool 3")]
        public static string Tool3(string input) => $"Tool3: {input}";

        [Tool(Description = "Tool 4")]
        public static string Tool4(string input) => $"Tool4: {input}";

        [Tool(Description = "Tool 5")]
        public static string Tool5(string input) => $"Tool5: {input}";

        // Simple data structure for performance testing
        public class SimpleInfo
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Occupation { get; set; } = string.Empty;
        }
    }
}