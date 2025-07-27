using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;

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
            Assert.Throws<ArgumentException>(() => new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                []));
        }

        [Test]
        public void LLMTornadoModelProvider_WithEmptyAuthentication_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                new List<ProviderAuthentication>()));
        }

        /// <summary>
        /// I do not Error Out on run only verbose logging of failed result.
        /// </summary>
        /// <returns></returns>
        //[Test]
        //public async Task RunAsync_WithInvalidApiKey_ShouldThrowException()
        //{
        //    // Arrange
        //    var invalidProvider = new LLMTornadoModelProvider(
        //        ChatModel.OpenAi.Gpt41.V41Mini,
        //        [new ProviderAuthentication(LLmProviders.OpenAi, "invalid-key")]);

        //    var agent = new Agent(invalidProvider, "Assistant", "You are a helpful assistant");

        //    // Act & Assert
        //    var ex = Assert.ThrowsAsync<Exception>(async () => await Runner.RunAsync(agent, "Hello"));
        //    ex.Should().NotBeNull();
        //}

        //[Test]
        //public async Task RunAsync_WithNullPrompt_ShouldThrowArgumentNullException()
        //{
        //    // Arrange
        //    SkipIfNoApiKey();
        //    if (_modelProvider == null) Assert.Fail("Model provider not initialized");

        //    var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");

        //    // Act & Assert
        //     Assert.ThrowsAsync<ArgumentNullException>(async () => await Runner.RunAsync(agent, null!));
        //}

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
            var ex = Assert.ThrowsAsync<TargetInvocationException>(async () => await ToolRunner.CallFuncToolAsync(agent, functionCall));
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
            var ex = Assert.ThrowsAsync<System.Text.Json.JsonException>(async () => await ToolRunner.CallFuncToolAsync(agent, functionCall));
        }

        [Test]
        public void ParseJson_WithMalformedJson_ShouldThrowJsonException()
        {
            // Arrange
            var runResult = new RunResult();
            runResult.Response = new ModelResponse();
            runResult.Response.OutputItems = new List<ModelItem>
            {
                new ModelMessageItem("msg_1", "assistant", 
                    new List<ModelMessageContent> { new ModelMessageAssistantResponseTextContent("{ invalid json }") }, 
                    ModelStatus.Completed)
            };

            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() => runResult.ParseJson<TestClass>());
        }

        [Test]
        public void ParseJson_WithIncompatibleSchema_ShouldThrowException()
        {
            // Arrange
            var validJson = "{ \"wrongProperty\": \"value\" }";
            var runResult = new RunResult();
            runResult.Response = new ModelResponse();
            runResult.Response.OutputItems = new List<ModelItem>
            {
                new ModelMessageItem("msg_1", "assistant", 
                    new List<ModelMessageContent> { new ModelMessageAssistantResponseTextContent(validJson) }, 
                    ModelStatus.Completed)
            };

            // Act - This should work as JSON deserialization is flexible
            var result = runResult.ParseJson<TestClass>();

            // Assert - Properties should be default values
            result.Should().NotBeNull();
            result.Name.Should().BeEmpty();
            result.Value.Should().Be(0);
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

        /// <summary>
        /// Again this will not throw an exception, but will log the error, I guess i could provide a safe vs unsafe mode to toggle this behavior.
        /// </summary>
        //[Test]
        //public async Task RunAsync_WithCancellationToken_ShouldRespectCancellation()
        //{
        //    // Arrange
        //    SkipIfNoApiKey();
        //    if (_modelProvider == null) Assert.Fail("Model provider not initialized");

        //    var agent = new Agent(_modelProvider, "Assistant", "You are a helpful assistant");
        //    using var cts = new CancellationTokenSource();
        //    cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel quickly

        //    // Set the cancellation token
        //    _modelProvider.CancelTokenSource = cts;

        //    // Act & Assert
        //    var ex = Assert.ThrowsAsync<OperationCanceledException>(
        //        async () => await Runner.RunAsync(agent, "Write a very long story"));
        //}

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

            Assert.Throws<ArgumentException>(() => new Agent(_modelProvider, "Assistant", "You are a helpful assistant",
                _output_schema: typeof(ComplexNestedStructure)));
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

        [System.ComponentModel.Description("Complex nested structure for testing error handling")]
        public class ComplexNestedStructure
        {
            [System.ComponentModel.Description("List of nested objects")]
            public NestedObject[] Items { get; set; } 

            [System.ComponentModel.Description("Dictionary of values")]
            public Dictionary<string, NestedObject> Map { get; set; } = new();

            [System.ComponentModel.Description("Array of arrays")]
            public string[][] Matrix { get; set; } = Array.Empty<string[]>();
        }

        [System.ComponentModel.Description("Nested object for complex structure")]
        public class NestedObject
        {
            [System.ComponentModel.Description("Name property")]
            public string Name { get; set; } = string.Empty;

            [System.ComponentModel.Description("Nested list")]
            public string[] Values { get; set; }

            [System.ComponentModel.Description("Optional nested object")]
            public NestedObject? Child { get; set; }
        }
    }
}