using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using ModelContextProtocol.Server;

namespace Test.Agents
{
    [TestFixture]
    public class AgentTests
    {
        private LLMTornadoModelProvider? _modelProvider;
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
                _modelProvider = new LLMTornadoModelProvider(
                    ChatModel.OpenAi.Gpt41.V41Nano,
                    [new ProviderAuthentication(LLmProviders.OpenAi, _apiKey)]);
            }
        }

        [Test]
        public void Constructor_WithMinimalParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider);

            // Assert
            agent.Should().NotBeNull();
            agent.AgentName.Should().Be("Assistant");
            agent.Instructions.Should().Be("You are a helpful assistant");
            agent.Client.Should().Be(_modelProvider);
            agent.Tools.Should().NotBeNull();
            agent.tool_list.Should().BeEmpty();
            agent.agent_tools.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithCustomParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
            var agentName = "TestAgent";
            var instructions = "You are a test assistant";

            // Act
            var agent = new Agent(_modelProvider, agentName, instructions);

            // Assert
            agent.AgentName.Should().Be(agentName);
            agent.Instructions.Should().Be(instructions);
            agent.Options.Instructions.Should().Be(instructions);
        }

        [Test]
        public void Constructor_WithOutputSchema_ShouldSetOutputFormat()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _output_schema: typeof(TestStructuredOutput));

            // Assert
            agent.OutputSchema.Should().Be(typeof(TestStructuredOutput));
            agent.Options.OutputFormat.Should().NotBeNull();
            agent.Options.OutputFormat!.JsonSchemaFormatName.Should().Be("TestStructuredOutput");
        }

        [Test]
        public void Constructor_WithTools_ShouldSetupToolsCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
            var tools = new List<Delegate> { TestFunction };

            // Act
            var agent = new Agent(_modelProvider, _tools: tools);

            // Assert
            agent.Tools.Should().HaveCount(1);
            agent.tool_list.Should().HaveCount(1);
            agent.tool_list.Should().ContainKey("TestFunction");
            agent.Options.Tools.Should().HaveCount(1);
        }

        [Test]
        public void Constructor_WithAgentTool_ShouldSetupAgentToolsCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
            var subAgent = new Agent(_modelProvider, "SubAgent", "I am a sub agent");
            var tools = new List<Delegate> { subAgent.AsTool };

            // Act
            var agent = new Agent(_modelProvider, _tools: tools);

            // Assert
            agent.agent_tools.Should().HaveCount(1);
            agent.agent_tools.Should().ContainKey("SubAgent");
            agent.Options.Tools.Should().HaveCount(1);
        }

        [Test]
        public void Constructor_WithMCPServers_ShouldSetupMCPCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
            var mcpServers = new List<MCPServer> 
            { 
                new MCPServer("test-server", "test-command")
            };

            // Act
            var agent = new Agent(_modelProvider, mcpServers: mcpServers);

            // Assert
            agent.MCPServers.Should().HaveCount(1);
            agent.Options.MCPServers.Should().HaveCount(1);
        }

        [Test]
        public void Constructor_WithEmptyInstructions_ShouldUseDefault()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _instructions: "");

            // Assert
            agent.Instructions.Should().Be("You are a helpful assistant");
        }

        [Test]
        public void Constructor_WithNullInstructions_ShouldUseDefault()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _instructions: null!);

            // Assert
            agent.Instructions.Should().Be("You are a helpful assistant");
        }

        [Test]
        public void DummyAgent_ShouldCreateValidAgent()
        {
            // Arrange & Act
            var agent = Agent.DummyAgent();

            // Assert
            agent.Should().NotBeNull();
            agent.AgentName.Should().Be("Assistant");
            agent.Client.Should().NotBeNull();
        }

        [Test]
        public void AsTool_ShouldReturnAgentTool()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
            var agent = new Agent(_modelProvider, "TestAgent", "Test instructions");

            // Act
            var agentTool = agent.AsTool();

            // Assert
            agentTool.Should().NotBeNull();
            agentTool.ToolAgent.Should().Be(agent);
            agentTool.Tool.Should().NotBeNull();
            agentTool.Tool.ToolName.Should().Be("TestAgent");
        }

        // Helper method for testing
        [Tool(Description = "A test function for testing purposes", In_parameters_description =["Input"])]
        public static string TestFunction(string input)
        {
            return $"Processed: {input}";
        }

        // Test structured output class
        [System.ComponentModel.Description("Test structured output for testing")]
        public class TestStructuredOutput
        {
            [System.ComponentModel.Description("Test property")]
            public string TestProperty { get; set; } = string.Empty;

            [System.ComponentModel.Description("Test number")]
            public int TestNumber { get; set; }
        }
    }
}