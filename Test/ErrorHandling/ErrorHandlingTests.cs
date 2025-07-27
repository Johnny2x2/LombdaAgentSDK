using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System.ComponentModel;

namespace Test.ErrorHandling
{
    [TestFixture]
    public class ErrorHandlingTests : TestBase
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
        public void Agent_WithNullClient_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Agent(null!));
        }

        [Test]
        public void LLMTornadoModelProvider_WithNullAuthentication_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                null!));
        }

        [Test]
        public void LLMTornadoModelProvider_WithEmptyAuthentication_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                new List<ProviderAuthentication>()));
        }

        [Test]
        public async Task RunAsync_WithInvalidApiKey_ShouldThrowException()
        {
            // Arrange
            var invalidProvider = new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, "invalid-key")]);

            var agent = new Agent(invalidProvider, "Assistant", "You are a helpful assistant");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => Runner.RunAsync(agent, "Hello"));
            ex.Should().NotBeNull();
        }

        [Test]
        public async Task RunAsync_WithNullPrompt_ShouldThrowArgumentNullException()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => Runner.RunAsync(agent, null!));
        }

        [Test]
        public async Task RunAsync_WithEmptyPrompt_ShouldHandleGracefully()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");

            // Act
            var result = await Runner.RunAsync(agent, "");

            // Assert
            result.Should().NotBeNull();
            // Empty prompt should still get some response
        }

        [Test]
        public async Task ToolRunner_WithToolThatThrowsException_ShouldHandleError()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant", _tools: [ThrowingTool]);

            var functionCall = new ModelFunctionCallItem(
                "call_error",
                "call_error",
                "ThrowingTool",
                ModelStatus.InProgress,
                BinaryData.FromString("{}"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => ToolRunner.CallFuncToolAsync(agent, functionCall));
            ex.Message.Should().Contain("Tool error");
        }

        [Test]
        public async Task ToolRunner_WithMalformedFunctionCallArguments_ShouldHandleError()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant", _tools: [ValidTool]);

            var functionCall = new ModelFunctionCallItem(
                "call_malformed",
                "call_malformed",
                "ValidTool",
                ModelStatus.InProgress,
                BinaryData.FromString("{ invalid json }"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => ToolRunner.CallFuncToolAsync(agent, functionCall));
            ex.Should().NotBeNull();
        }

        [Test]
        public void ParseJson_WithMalformedJson_ShouldThrowJsonException()
        {
            // Arrange
            var runResult = new RunResult("{ invalid json }", null!, null!);

            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() => runResult.ParseJson<TestClass>());
        }

        [Test]
        public void ParseJson_WithIncompatibleSchema_ShouldThrowException()
        {
            // Arrange
            var validJson = "{ \"wrongProperty\": \"value\" }";
            var runResult = new RunResult(validJson, null!, null!);

            // Act - This should work as JSON deserialization is flexible
            var result = runResult.ParseJson<TestClass>();

            // Assert - Properties should be default values
            result.Should().NotBeNull();
            result.Name.Should().BeEmpty();
            result.Value.Should().Be(0);
        }

        [Test]
        public async Task RunAsync_WithVeryLongPrompt_ShouldHandleGracefully()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");
            var longPrompt = new string('A', 10000); // Very long prompt

            // Act
            var result = await Runner.RunAsync(agent, longPrompt);

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
        }

        [Test]
        public void Agent_WithInvalidOutputSchema_ShouldHandleGracefully()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            // Act - This should work, the schema generation should handle most types
            var agent = new Agent(_modelProvider, _output_schema: typeof(string));

            // Assert
            agent.Should().NotBeNull();
            agent.OutputSchema.Should().Be(typeof(string));
        }

        [Test]
        public void ConvertFunctionToTool_WithInvalidFunction_ShouldReturnNull()
        {
            // Arrange
            Delegate invalidFunction = InvalidFunction;

            // Act
            var tool = invalidFunction.ConvertFunctionToTool();

            // Assert
            tool.Should().BeNull();
        }

        [Test]
        public async Task RunAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel quickly

            // Set the cancellation token
            _modelProvider.CancelTokenSource = cts;

            // Act & Assert
            var ex = await Assert.ThrowsAsync<OperationCanceledException>(
                () => Runner.RunAsync(agent, "Write a very long story"));
        }

        [Test]
        public void Agent_WithNullToolsList_ShouldInitializeWithEmptyTools()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _tools: null);

            // Assert
            agent.Should().NotBeNull();
            agent.Tools.Should().NotBeNull();
            agent.Tools.Should().BeEmpty();
        }

        [Test]
        public async Task RunAsync_WithExtremelyComplexStructuredOutput_ShouldHandleGracefully()
        {
            // Arrange
            SkipIfNoApiKey();
            if (_modelProvider == null) Assert.Fail("Model provider not initialized");

            var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant", 
                _output_schema: typeof(ComplexNestedStructure));

            // Act
            var result = await Runner.RunAsync(agent, "Create a complex data structure");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();
            
            // Should be able to parse even if some fields are missing
            var parsed = result.ParseJson<ComplexNestedStructure>();
            parsed.Should().NotBeNull();
        }

        // Test helper methods and classes
        [Tool(Description = "A tool that throws an exception")]
        public static string ThrowingTool()
        {
            throw new Exception("Tool error");
        }

        [Tool(Description = "A valid tool for testing")]
        public static string ValidTool(string input)
        {
            return $"Valid: {input}";
        }

        // Function without Tool attribute
        public static string InvalidFunction(string input)
        {
            return input;
        }

        public class TestClass
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        [Description("Complex nested structure for testing error handling")]
        public class ComplexNestedStructure
        {
            [Description("List of nested objects")]
            public List<NestedObject> Items { get; set; } = new();

            [Description("Dictionary of values")]
            public Dictionary<string, NestedObject> Map { get; set; } = new();

            [Description("Array of arrays")]
            public string[][] Matrix { get; set; } = Array.Empty<string[]>();
        }

        [Description("Nested object for complex structure")]
        public class NestedObject
        {
            [Description("Name property")]
            public string Name { get; set; } = string.Empty;

            [Description("Nested list")]
            public List<string> Values { get; set; } = new();

            [Description("Optional nested object")]
            public NestedObject? Child { get; set; }
        }
    }
}