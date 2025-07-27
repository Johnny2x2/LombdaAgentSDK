using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System.Text.Json;

namespace Test.Tools
{
    [TestFixture]
    public class ToolRunnerTests
    {
        private Agent? _agent;
        private string? _apiKey;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        [SetUp]
        public void SetUp()
        {
            if (!string.IsNullOrEmpty(_apiKey))
            {
                var modelProvider = new LLMTornadoModelProvider(
                    ChatModel.OpenAi.Gpt41.V41Nano,
                    [new ProviderAuthentication(LLmProviders.OpenAi, _apiKey)]);

                _agent = new Agent(modelProvider, _tools: [
                    TestStringFunction,
                    TestIntFunction,
                    TestAsyncFunction,
                    TestComplexFunction
                ]);
            }
        }

        [Test]
        public async Task CallFuncToolAsync_WithStringFunction_ShouldReturnCorrectResult()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var functionCall = new ModelFunctionCallItem(
                "call_123",
                "call_123",
                "TestStringFunction",
                ModelStatus.InProgress,
                BinaryData.FromString(JsonSerializer.Serialize(new { input = "test input" })));

            // Act
            var result = await ToolRunner.CallFuncToolAsync(_agent, functionCall);

            // Assert
            result.Should().NotBeNull();
            result.FunctionName.Should().Be("TestStringFunction");
            result.FunctionOutput.Should().Be("Processed: test input");
            result.CallId.Should().Be("call_123");
            result.Status.Should().Be(ModelStatus.InProgress);
        }

        [Test]
        public async Task CallFuncToolAsync_WithIntFunction_ShouldReturnCorrectResult()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var functionCall = new ModelFunctionCallItem(
                "call_456",
                "call_456",
                "TestIntFunction",
                ModelStatus.InProgress,
                BinaryData.FromString(JsonSerializer.Serialize(new { number = 42 })));

            // Act
            var result = await ToolRunner.CallFuncToolAsync(_agent, functionCall);

            // Assert
            result.Should().NotBeNull();
            result.FunctionName.Should().Be("TestIntFunction");
            result.FunctionOutput.Should().Be("84");
            result.CallId.Should().Be("call_456");
        }

        [Test]
        public async Task CallFuncToolAsync_WithAsyncFunction_ShouldReturnCorrectResult()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var functionCall = new ModelFunctionCallItem(
                "call_789",
                "call_789",
                "TestAsyncFunction",
                ModelStatus.InProgress,
                BinaryData.FromString(JsonSerializer.Serialize(new { input = "async test" })));

            // Act
            var result = await ToolRunner.CallFuncToolAsync(_agent, functionCall);

            // Assert
            result.Should().NotBeNull();
            result.FunctionName.Should().Be("TestAsyncFunction");
            result.FunctionOutput.Should().Be("Async processed: async test");
            result.CallId.Should().Be("call_789");
        }

        [Test]
        public async Task CallFuncToolAsync_WithComplexFunction_ShouldReturnCorrectResult()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var functionCall = new ModelFunctionCallItem(
                "call_complex",
                "call_complex",
                "TestComplexFunction",
                ModelStatus.InProgress,
                BinaryData.FromString(JsonSerializer.Serialize(new { 
                    name = "John", 
                    age = 30, 
                    isActive = true 
                })));

            // Act
            var result = await ToolRunner.CallFuncToolAsync(_agent, functionCall);

            // Assert
            result.Should().NotBeNull();
            result.FunctionName.Should().Be("TestComplexFunction");
            result.FunctionOutput.Should().Be("Name: John, Age: 30, Active: True");
            result.CallId.Should().Be("call_complex");
        }


        [Test]
        public async Task CallFuncToolAsync_WithNoArguments_ShouldCallFunctionWithoutArgs()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var noArgsAgent = new Agent(_agent.Client, _tools: [TestNoArgsFunction]);

            var functionCall = new ModelFunctionCallItem(
                "call_noargs",
                "call_noargs",
                "TestNoArgsFunction",
                ModelStatus.InProgress,
                null);

            // Act
            var result = await ToolRunner.CallFuncToolAsync(noArgsAgent, functionCall);

            // Assert
            result.Should().NotBeNull();
            result.FunctionName.Should().Be("TestNoArgsFunction");
            result.FunctionOutput.Should().Be("No arguments function called");
        }

        [Test]
        public async Task CallAgentToolAsync_WithValidAgentTool_ShouldReturnResult()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var subAgent = new Agent(_agent.Client, "SubAgent", "I translate to uppercase");
            var agentWithSubAgent = new Agent(_agent.Client, _tools: [subAgent.AsTool]);

            var functionCall = new ModelFunctionCallItem(
                "call_agent",
                "call_agent",
                "SubAgent",
                ModelStatus.InProgress,
                BinaryData.FromString(JsonSerializer.Serialize(new { input = "hello world" })));

            // Act
            var result = await ToolRunner.CallAgentToolAsync(agentWithSubAgent, functionCall);

            // Assert
            result.Should().NotBeNull();
            result.FunctionName.Should().Be("SubAgent");
            result.CallId.Should().Be("call_agent");
            result.FunctionOutput.Should().NotBeEmpty();
        }

        [Test]
        public async Task CallAgentToolAsync_WithNonExistentAgentTool_ShouldThrowException()
        {
            // Arrange
            if (_agent == null) Assert.Ignore("Agent not initialized");

            var functionCall = new ModelFunctionCallItem(
                "call_nonexistent_agent",
                "call_nonexistent_agent",
                "NonExistentAgent",
                ModelStatus.InProgress,
                BinaryData.FromString(JsonSerializer.Serialize(new { input = "test" })));

            // Act & Assert
            var ex =  Assert.ThrowsAsync<Exception>(
                async() => await ToolRunner.CallAgentToolAsync(_agent, functionCall));

            ex?.Message.Should().Contain("I don't have a Agent tool called NonExistentAgent");
        }

        // Test functions for the ToolRunner tests
        [Tool(Description = "Test function that processes string input")]
        public static string TestStringFunction(string input)
        {
            return $"Processed: {input}";
        }

        [Tool(Description = "Test function that doubles an integer")]
        public static int TestIntFunction(int number)
        {
            return number * 2;
        }

        [Tool(Description = "Test async function")]
        public static async Task<string> TestAsyncFunction(string input)
        {
            await Task.Delay(10); // Simulate async work
            return $"Async processed: {input}";
        }

        [Tool(Description = "Test function with multiple parameters")]
        public static string TestComplexFunction(string name, int age, bool isActive)
        {
            return $"Name: {name}, Age: {age}, Active: {isActive}";
        }

        [Tool(Description = "Test function with no arguments")]
        public static string TestNoArgsFunction()
        {
            return "No arguments function called";
        }
    }
}