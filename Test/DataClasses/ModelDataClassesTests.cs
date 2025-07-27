using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents;
using System.Text.Json;

namespace Test.DataClasses
{
    [TestFixture]
    public class ModelDataClassesTests
    {
        [Test]
        public void ModelMessageItem_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var id = "msg_123";
            var role = "user";
            var text = "Hello, world!";
            var content = new List<ModelMessageContent>
            {
                new ModelMessageRequestTextContent(text)
            };

            // Act
            var messageItem = new ModelMessageItem(id, role, content, ModelStatus.Completed);

            // Assert
            messageItem.Should().NotBeNull();
            messageItem.Id.Should().Be(id);
            messageItem.Role.Should().Be(role);
            messageItem.Text.Should().Be(text);
            messageItem.Status.Should().Be(ModelStatus.Completed);
        }

        [Test]
        public void ModelFunctionCallItem_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var id = "call_123";
            var callId = "call_123";
            var functionName = "TestFunction";
            var status = ModelStatus.InProgress;
            var arguments = BinaryData.FromString(JsonSerializer.Serialize(new { input = "test" }));

            // Act
            var functionCallItem = new ModelFunctionCallItem(id, callId, functionName, status, arguments);

            // Assert
            functionCallItem.Should().NotBeNull();
            functionCallItem.Id.Should().Be(id);
            functionCallItem.CallId.Should().Be(callId);
            functionCallItem.FunctionName.Should().Be(functionName);
            functionCallItem.Status.Should().Be(status);
            functionCallItem.FunctionArguments.Should().Be(arguments);
        }

        [Test]
        public void ModelFunctionCallOutputItem_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var id = "output_123";
            var callId = "call_123";
            var output = "Function executed successfully";
            var status = ModelStatus.Completed;
            var functionName = "TestFunction";

            // Act
            var outputItem = new ModelFunctionCallOutputItem(id, callId, output, status, functionName);

            // Assert
            outputItem.Should().NotBeNull();
            outputItem.Id.Should().Be(id);
            outputItem.CallId.Should().Be(callId);
            outputItem.FunctionOutput.Should().Be(output);
            outputItem.Status.Should().Be(status);
            outputItem.FunctionName.Should().Be(functionName);
        }

        [Test]
        public void ModelResponse_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var outputItems = new List<ModelItem>
            {
                new ModelMessageItem("msg_1", "assistant", 
                    new List<ModelMessageContent> { new ModelMessageAssistantResponseTextContent("Hello!") }, 
                    ModelStatus.Completed)
            };
            var messages = new List<ModelItem>
            {
                new ModelMessageItem("msg_2", "user", 
                    new List<ModelMessageContent> { new ModelMessageRequestTextContent("Hi") }, 
                    ModelStatus.Completed),
                new ModelMessageItem("msg_3", "assistant", 
                    new List<ModelMessageContent> { new ModelMessageAssistantResponseTextContent("Hello!") }, 
                    ModelStatus.Completed)
            };
            var id = "response_123";

            // Act
            var response = new ModelResponse(outputItems, null, messages, id);

            // Assert
            response.Should().NotBeNull();
            response.OutputItems.Should().BeEquivalentTo(outputItems);
            response.Messages.Should().BeEquivalentTo(messages);
            response.Id.Should().Be(id);
        }

        [Test]
        public void ModelResponseOptions_DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var options = new ModelResponseOptions();

            // Assert
            options.Should().NotBeNull();
            options.Tools.Should().NotBeNull();
            options.Tools.Should().BeEmpty();
            options.MCPServers.Should().NotBeNull();
            options.MCPServers.Should().BeEmpty();
            options.Instructions.Should().BeNull();
            options.Model.Should().BeNull();
            options.OutputFormat.Should().BeNull();
            options.ReasoningOptions.Should().BeNull();
            options.PreviousResponseId.Should().BeNull();
        }

        [Test]
        public void ModelResponseOptions_WithCustomValues_ShouldSetCorrectly()
        {
            // Arrange
            var instructions = "Custom instructions";
            var model = "gpt-4";
            var outputFormat = new ModelOutputFormat("TestFormat", BinaryData.FromString("{}"), true);
            var reasoningOptions = new ModelReasoningOptions
            {
                EffortLevel = ModelReasoningEffortLevel.High
            };

            // Act
            var options = new ModelResponseOptions
            {
                Instructions = instructions,
                Model = model,
                OutputFormat = outputFormat,
                ReasoningOptions = reasoningOptions
            };

            // Assert
            options.Instructions.Should().Be(instructions);
            options.Model.Should().Be(model);
            options.OutputFormat.Should().Be(outputFormat);
            options.ReasoningOptions.Should().Be(reasoningOptions);
            options.ReasoningOptions.EffortLevel.Should().Be(ModelReasoningEffortLevel.High);
        }

        [Test]
        public void ModelOutputFormat_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var formatName = "TestFormat";
            var schema = BinaryData.FromString("{ \"type\": \"object\" }");
            var isStrict = true;
            var description = "Test format description";

            // Act
            var outputFormat = new ModelOutputFormat(formatName, schema, isStrict, description);

            // Assert
            outputFormat.Should().NotBeNull();
            outputFormat.JsonSchemaFormatName.Should().Be(formatName);
            outputFormat.JsonSchema.Should().Be(schema);
            outputFormat.JsonSchemaIsStrict.Should().Be(isStrict);
            outputFormat.FormatDescription.Should().Be(description);
        }

        [Test]
        public void ModelReasoningOptions_DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var options = new ModelReasoningOptions();

            // Assert
            options.Should().NotBeNull();
            options.EffortLevel.Should().Be(ModelReasoningEffortLevel.Medium);
        }

        [Test]
        [TestCase(ModelReasoningEffortLevel.Low)]
        [TestCase(ModelReasoningEffortLevel.Medium)]
        [TestCase(ModelReasoningEffortLevel.High)]
        public void ModelReasoningOptions_WithDifferentEffortLevels_ShouldSetCorrectly(ModelReasoningEffortLevel level)
        {
            // Act
            var options = new ModelReasoningOptions { EffortLevel = level };

            // Assert
            options.EffortLevel.Should().Be(level);
        }

        [Test]
        public void RunResult_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var response = new ModelResponse(new List<ModelItem>(), null, new List<ModelItem>());
            var messages = new List<ModelItem>
            {
                new ModelMessageItem("msg_1", "user", 
                    new List<ModelMessageContent> { new ModelMessageRequestTextContent("Test message") }, 
                    ModelStatus.Completed)
            };

            // Act
            var runResult = new RunResult();
            runResult.Response = response;
            runResult.Messages = messages;

            // Assert
            runResult.Should().NotBeNull();
            runResult.Response.Should().Be(response);
            runResult.Messages.Should().BeEquivalentTo(messages);
        }

        [Test]
        public void VectorSearchOptions_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var vectorIds = new List<string> { "vector1", "vector2", "vector3" };

            // Act
            var options = new VectorSearchOptions { VectorIDs = vectorIds };

            // Assert
            options.Should().NotBeNull();
            options.VectorIDs.Should().BeEquivalentTo(vectorIds);
        }

        [Test]
        public void ModelCodeInterpreterOptions_WithFileIds_ShouldInitializeCorrectly()
        {
            // Arrange
            var fileIds = new List<string> { "file1", "file2" };

            // Act
            var options = new ModelCodeInterpreterOptions { FileIds = fileIds };

            // Assert
            options.Should().NotBeNull();
            options.FileIds.Should().BeEquivalentTo(fileIds);
            options.ContainerId.Should().BeEmpty();
        }

        [Test]
        public void ModelCodeInterpreterOptions_WithContainerId_ShouldInitializeCorrectly()
        {
            // Arrange
            var containerId = "container123";

            // Act
            var options = new ModelCodeInterpreterOptions { ContainerId = containerId };

            // Assert
            options.Should().NotBeNull();
            options.ContainerId.Should().Be(containerId);
            options.FileIds.Should().BeEmpty();
        }

        [Test]
        [TestCase(ModelStatus.InProgress)]
        [TestCase(ModelStatus.Completed)]
        [TestCase(ModelStatus.Incomplete)]
        public void ModelStatus_EnumValues_ShouldBeValid(ModelStatus status)
        {
            // Act & Assert
            Enum.IsDefined(typeof(ModelStatus), status).Should().BeTrue();
        }
    }
}