using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System.Text.Json;

namespace Test.LLMTornado
{
    [TestFixture]
    public class LLMTornadoModelProviderTests
    {
        private LLMTornadoModelProvider? _modelProvider;
        private string? _apiKey;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
            {
                Assert.Ignore("OpenAI API key not found in environment variables. Skipping LLM Tornado integration tests.");
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
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var provider = new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, "test-key")],
                true);

            // Assert
            provider.Should().NotBeNull();
            provider.CurrentModel.Should().Be(ChatModel.OpenAi.Gpt41.V41Mini);
            provider.UseResponseAPI.Should().BeTrue();
            provider.Model.Should().Be(ChatModel.OpenAi.Gpt41.V41Mini.Name);
        }


        [Test]
        public void SetupClient_WithValidOptions_ShouldConfigureConversation()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var messages = new List<ModelItem>
            {
                new ModelMessageItem("msg_1", "user", 
                    new List<ModelMessageContent> { new ModelMessageRequestTextContent("Hello, world!") }, 
                    ModelStatus.Completed)
            };
            var options = new ModelResponseOptions
            {
                Instructions = "You are a helpful assistant"
            };
            var conversation = _modelProvider.Client.Chat.CreateConversation(_modelProvider.CurrentModel);

            // Act
            var result = _modelProvider.SetupClient(conversation, messages, options);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(conversation);
        }

        [Test]
        public void SetupResponseClient_WithValidOptions_ShouldConfigureRequest()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var messages = new List<ModelItem>
            {
                new ModelMessageItem("msg_1", "user", 
                    new List<ModelMessageContent> { new ModelMessageRequestTextContent("Hello, world!") }, 
                    ModelStatus.Completed)
            };
            var options = new ModelResponseOptions
            {
                Instructions = "You are a helpful assistant"
            };

            // Act
            var result = _modelProvider.SetupResponseClient(messages, options);

            // Assert
            result.Should().NotBeNull();
            result.Model.Should().Be(_modelProvider.CurrentModel);
            result.Instructions.Should().Be(options.Instructions);
            result.InputItems.Should().HaveCount(1);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Constructor_WithResponseAPIFlag_ShouldSetCorrectly(bool useResponseAPI)
        {
            // Arrange & Act
            var provider = new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, "test-key")],
                useResponseAPI);

            // Assert
            provider.UseResponseAPI.Should().Be(useResponseAPI);
        }

        [Test]
        public void Constructor_WithAllOptionalParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var vectorOptions = new VectorSearchOptions { VectorIDs = ["vector1"] };
            var codeOptions = new ModelCodeInterpreterOptions { FileIds = ["file1"] };

            // Act
            var provider = new LLMTornadoModelProvider(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, "test-key")],
                useResponseAPI: false,
                allowComputerUse: true,
                searchOptions: vectorOptions,
                enableWebSearch: true,
                codeOptions: codeOptions);

            // Assert
            provider.Should().NotBeNull();
            provider.AllowComputerUse.Should().BeTrue();
            provider.VectorSearchOptions.Should().Be(vectorOptions);
            provider.EnableWebSearch.Should().BeTrue();
            provider.CodeOptions.Should().Be(codeOptions);
        }
    }
}