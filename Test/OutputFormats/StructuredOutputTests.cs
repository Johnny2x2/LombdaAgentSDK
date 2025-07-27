using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System.ComponentModel;
using System.Text.Json;

namespace Test.OutputFormats
{
    [TestFixture]
    public class StructuredOutputTests
    {
        private LLMTornadoModelProvider? _modelProvider;
        private string? _apiKey;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
            {
                Assert.Ignore("OpenAI API key not found in environment variables. Skipping structured output integration tests.");
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
        public void Agent_WithStructuredOutput_ShouldSetOutputFormatCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _output_schema: typeof(PersonInfo));

            // Assert
            agent.OutputSchema.Should().Be(typeof(PersonInfo));
            agent.Options.OutputFormat.Should().NotBeNull();
            agent.Options.OutputFormat!.JsonSchemaFormatName.Should().Be("PersonInfo");
            agent.Options.OutputFormat.JsonSchemaIsStrict.Should().BeTrue();
        }

        [Test]
        public async Task RunAgent_WithStructuredOutput_ShouldReturnValidJson()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a helpful assistant that extracts person information",
                _output_schema: typeof(PersonInfo));

            // Act
            var result = await Runner.RunAsync(agent, "Extract information about John Doe, a 30 year old software engineer from New York");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();

            // Verify it's valid JSON
            var personInfo = result.ParseJson<PersonInfo>();
            personInfo.Should().NotBeNull();
            personInfo.Name.Should().NotBeEmpty();
            personInfo.Age.Should().BeGreaterThan(0);
        }

        [Test]
        public void Agent_WithComplexStructuredOutput_ShouldSetOutputFormatCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _output_schema: typeof(MathReasoning));

            // Assert
            agent.OutputSchema.Should().Be(typeof(MathReasoning));
            agent.Options.OutputFormat.Should().NotBeNull();
            agent.Options.OutputFormat!.JsonSchemaFormatName.Should().Be("MathReasoning");
        }

        [Test]
        public async Task RunAgent_WithMathReasoning_ShouldReturnValidStructuredOutput()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a math tutor that explains step-by-step solutions",
                _output_schema: typeof(MathReasoning));

            // Act
            var result = await Runner.RunAsync(agent, "Solve: 2x + 5 = 15");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();

            // Verify it's valid JSON and has expected structure
            var mathResult = result.ParseJson<MathReasoning>();
            mathResult.Should().NotBeNull();
            mathResult.FinalAnswer.Should().NotBeEmpty();
            mathResult.Steps.Should().NotBeEmpty();
            mathResult.Steps.Should().HaveCountGreaterThan(0);
        }

        [Test]
        public void Agent_WithNestedStructuredOutput_ShouldSetOutputFormatCorrectly()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            // Act
            var agent = new Agent(_modelProvider, _output_schema: typeof(Company));

            // Assert
            agent.OutputSchema.Should().Be(typeof(Company));
            agent.Options.OutputFormat.Should().NotBeNull();
        }

        [Test]
        public async Task RunAgent_WithNestedStructuredOutput_ShouldReturnValidNestedJson()
        {
            // Arrange
            if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

            var agent = new Agent(
                _modelProvider,
                "Assistant",
                "You are a business analyst that extracts company information",
                _output_schema: typeof(Company));

            // Act
            var result = await Runner.RunAsync(agent, "Analyze Microsoft Corporation: a technology company founded in 1975 by Bill Gates and Paul Allen, headquartered in Redmond, Washington");

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().NotBeEmpty();

            // Verify nested structure
            var company = result.ParseJson<Company>();
            company.Should().NotBeNull();
            company.Name.Should().NotBeEmpty();
            company.Employees.Should().NotBeNull();
            company.Employees.Should().HaveCountGreaterThanOrEqualTo(0);
        }

        [Test]
        public void CreateJsonSchemaFormatFromType_WithSimpleType_ShouldCreateValidFormat()
        {
            // Arrange
            var type = typeof(PersonInfo);

            // Act
            var format = type.CreateJsonSchemaFormatFromType(true);

            // Assert
            format.Should().NotBeNull();
            format.JsonSchemaFormatName.Should().Be("PersonInfo");
            format.JsonSchemaIsStrict.Should().BeTrue();
            format.JsonSchema.Should().NotBeNull();

            // Verify the schema contains expected properties
            var schemaJson = format.JsonSchema.ToString();
            schemaJson.Should().Contain("name");
            schemaJson.Should().Contain("age");
            schemaJson.Should().Contain("occupation");
        }

        [Test]
        public void CreateJsonSchemaFormatFromType_WithComplexType_ShouldCreateValidFormat()
        {
            // Arrange
            var type = typeof(MathReasoning);

            // Act
            var format = type.CreateJsonSchemaFormatFromType(false);

            // Assert
            format.Should().NotBeNull();
            format.JsonSchemaFormatName.Should().Be("MathReasoning");
            format.JsonSchemaIsStrict.Should().BeFalse();
            format.JsonSchema.Should().NotBeNull();

            var schemaJson = format.JsonSchema.ToString();
            schemaJson.Should().Contain("final_answer");
            schemaJson.Should().Contain("steps");
        }

        // Test data structures for structured output testing
        [Description("Information about a person")]
        public class PersonInfo
        {
            [Description("Full name of the person")]
            public string Name { get; set; } = string.Empty;

            [Description("Age in years")]
            public int Age { get; set; }

            [Description("Professional occupation")]
            public string Occupation { get; set; } = string.Empty;

            [Description("City where the person lives")]
            public string City { get; set; } = string.Empty;
        }

        [Description("Step-by-step mathematical reasoning")]
        public class MathReasoning
        {
            [Description("Array of reasoning steps")]
            public MathStep[] Steps { get; set; } = Array.Empty<MathStep>();

            [Description("Final answer to the problem")]
            public string FinalAnswer { get; set; } = string.Empty;
        }

        [Description("A single step in mathematical reasoning")]
        public class MathStep
        {
            [Description("Explanation of this step")]
            public string Explanation { get; set; } = string.Empty;

            [Description("Mathematical operation or result")]
            public string Operation { get; set; } = string.Empty;
        }

        [Description("Company information")]
        public class Company
        {
            [Description("Company name")]
            public string Name { get; set; } = string.Empty;

            [Description("Year founded")]
            public int FoundedYear { get; set; }

            [Description("Company headquarters location")]
            public Address Headquarters { get; set; } = new();

            [Description("List of employees")]
            public List<Employee> Employees { get; set; } = new();
        }

        [Description("Address information")]
        public class Address
        {
            [Description("Street address")]
            public string Street { get; set; } = string.Empty;

            [Description("City name")]
            public string City { get; set; } = string.Empty;

            [Description("State or province")]
            public string State { get; set; } = string.Empty;

            [Description("Country")]
            public string Country { get; set; } = string.Empty;
        }

        [Description("Employee information")]
        public class Employee
        {
            [Description("Employee name")]
            public string Name { get; set; } = string.Empty;

            [Description("Job title")]
            public string Title { get; set; } = string.Empty;

            [Description("Department")]
            public string Department { get; set; } = string.Empty;
        }
    }
}