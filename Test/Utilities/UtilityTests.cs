using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using System.ComponentModel;
using System.Text.Json;

namespace Test.Utilities
{
    [TestFixture]
    public class UtilityTests
    {
        [Test]
        public void ConvertFunctionToTool_WithSimpleFunction_ShouldCreateValidTool()
        {
            // Arrange
            Delegate testFunction = TestStringFunction;

            // Act
            var tool = testFunction.ConvertFunctionToTool();

            // Assert
            tool.Should().NotBeNull();
            tool!.ToolName.Should().Be("TestStringFunction");
            tool.ToolDescription.Should().Be("Test function that processes string input");
            tool.Function.Should().Be(testFunction);
        }

        [Test]
        public void ConvertFunctionToTool_WithComplexFunction_ShouldCreateValidTool()
        {
            // Arrange
            Delegate complexFunction = TestComplexFunction;

            // Act
            var tool = complexFunction.ConvertFunctionToTool();

            // Assert
            tool.Should().NotBeNull();
            tool!.ToolName.Should().Be("TestComplexFunction");
            tool.ToolDescription.Should().Be("Test function with multiple parameters");
            
            // Verify parameters are properly captured
            var parametersJson = tool.ToolParameters.ToString();
            parametersJson.Should().Contain("name");
            parametersJson.Should().Contain("age");
            parametersJson.Should().Contain("isActive");
        }

        [Test]
        public void ConvertFunctionToTool_WithAsyncFunction_ShouldCreateValidTool()
        {
            // Arrange
            Delegate asyncFunction = TestAsyncFunction;

            // Act
            var tool = asyncFunction.ConvertFunctionToTool();

            // Assert
            tool.Should().NotBeNull();
            tool!.ToolName.Should().Be("TestAsyncFunction");
            tool.ToolDescription.Should().Be("Test async function");
        }

        [Test]
        public void ConvertFunctionToTool_WithNoToolAttribute_ShouldReturnNull()
        {
            // Arrange
            Delegate functionWithoutAttribute = FunctionWithoutToolAttribute;

            // Act
            var tool = functionWithoutAttribute.ConvertFunctionToTool();

            // Assert
            tool.Should().BeNull();
        }

        [Test]
        public void ParseFunctionCallArgs_WithValidJson_ShouldParseCorrectly()
        {
            // Arrange
            Delegate testFunction = TestComplexFunction;
            var jsonArgs = BinaryData.FromString(JsonSerializer.Serialize(new
            {
                name = "John",
                age = 30,
                isActive = true
            }));

            // Act
            var parsedArgs = testFunction.ParseFunctionCallArgs(jsonArgs);

            // Assert
            parsedArgs.Should().NotBeNull();
            parsedArgs.Should().HaveCount(3);
            parsedArgs![0].Should().Be("John");
            parsedArgs[1].Should().Be(30);
            parsedArgs[2].Should().Be(true);
        }

        [Test]
        public void ParseFunctionCallArgs_WithPartialArgs_ShouldUseDefaults()
        {
            // Arrange
            Delegate functionWithDefaults = TestFunctionWithDefaults;
            var jsonArgs = BinaryData.FromString(JsonSerializer.Serialize(new
            {
                required = "test"
                // optional parameter omitted
            }));

            // Act
            var parsedArgs = functionWithDefaults.ParseFunctionCallArgs(jsonArgs);

            // Assert
            parsedArgs.Should().NotBeNull();
            parsedArgs.Should().HaveCount(2);
            parsedArgs![0].Should().Be("test");
            parsedArgs[1].Should().Be("default"); // Should use default value
        }

        [Test]
        public void CreateJsonSchemaFormatFromType_WithDescriptionAttribute_ShouldIncludeDescription()
        {
            // Arrange
            var type = typeof(TestDataClass);

            // Act
            var format = type.CreateJsonSchemaFormatFromType(true);

            // Assert
            format.Should().NotBeNull();
            format.JsonSchemaFormatName.Should().Be("TestDataClass");
            format.FormatDescription.Should().Be("Test data class for utility testing");
            format.JsonSchemaIsStrict.Should().BeTrue();
        }

        [Test]
        public void CreateJsonSchemaFormatFromType_WithoutDescription_ShouldHaveEmptyDescription()
        {
            // Arrange
            var type = typeof(TestDataClassWithoutDescription);

            // Act
            var format = type.CreateJsonSchemaFormatFromType(false);

            // Assert
            format.Should().NotBeNull();
            format.JsonSchemaFormatName.Should().Be("TestDataClassWithoutDescription");
            format.FormatDescription.Should().BeEmpty();
            format.JsonSchemaIsStrict.Should().BeFalse();
        }

        [Test]
        public void ParseJson_WithValidJsonString_ShouldDeserializeCorrectly()
        {
            // Arrange
            var testObject = new TestDataClass
            {
                Name = "Test Name",
                Value = 42,
                IsActive = true
            };
            var jsonString = JsonSerializer.Serialize(testObject);
            var runResult = new RunResult(jsonString, null!, null!);

            // Act
            var parsedObject = runResult.ParseJson<TestDataClass>();

            // Assert
            parsedObject.Should().NotBeNull();
            parsedObject.Name.Should().Be("Test Name");
            parsedObject.Value.Should().Be(42);
            parsedObject.IsActive.Should().BeTrue();
        }

        [Test]
        public void ParseJson_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var runResult = new RunResult(invalidJson, null!, null!);

            // Act & Assert
            Assert.Throws<JsonException>(() => runResult.ParseJson<TestDataClass>());
        }

        [Test]
        public void ParseJson_WithNullText_ShouldThrowException()
        {
            // Arrange
            var runResult = new RunResult(null!, null!, null!);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => runResult.ParseJson<TestDataClass>());
        }

        // Test functions for utility testing
        [Tool(Description = "Test function that processes string input")]
        public static string TestStringFunction(string input)
        {
            return $"Processed: {input}";
        }

        [Tool(Description = "Test function with multiple parameters")]
        public static string TestComplexFunction(string name, int age, bool isActive)
        {
            return $"Name: {name}, Age: {age}, Active: {isActive}";
        }

        [Tool(Description = "Test async function")]
        public static async Task<string> TestAsyncFunction(string input)
        {
            await Task.Delay(1);
            return $"Async: {input}";
        }

        [Tool(Description = "Test function with default parameters")]
        public static string TestFunctionWithDefaults(string required, string optional = "default")
        {
            return $"Required: {required}, Optional: {optional}";
        }

        // Function without Tool attribute for testing
        public static string FunctionWithoutToolAttribute(string input)
        {
            return input;
        }

        // Test data classes
        [Description("Test data class for utility testing")]
        public class TestDataClass
        {
            [Description("Name property")]
            public string Name { get; set; } = string.Empty;

            [Description("Value property")]
            public int Value { get; set; }

            [Description("IsActive property")]
            public bool IsActive { get; set; }
        }

        public class TestDataClassWithoutDescription
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}