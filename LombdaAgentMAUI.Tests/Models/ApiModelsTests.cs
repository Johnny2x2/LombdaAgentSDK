using LombdaAgentMAUI.Core.Models;

namespace LombdaAgentMAUI.Tests.Models
{
    [TestFixture]
    public class ApiModelsTests
    {
        [TestFixture]
        public class AgentCreationRequestTests
        {
            [Test]
            public void DefaultConstructor_SetsDefaultName()
            {
                // Act
                var request = new AgentCreationRequest();

                // Assert
                Assert.That(request.Name, Is.EqualTo("Assistant"));
            }

            [Test]
            public void Name_CanBeSet()
            {
                // Arrange
                var request = new AgentCreationRequest();
                var testName = "Custom Agent";

                // Act
                request.Name = testName;

                // Assert
                Assert.That(request.Name, Is.EqualTo(testName));
            }
        }

        [TestFixture]
        public class AgentResponseTests
        {
            [Test]
            public void DefaultConstructor_SetsEmptyValues()
            {
                // Act
                var response = new AgentResponse();

                // Assert
                Assert.That(response.Id, Is.EqualTo(string.Empty));
                Assert.That(response.Name, Is.EqualTo(string.Empty));
            }

            [Test]
            public void Properties_CanBeSet()
            {
                // Arrange
                var response = new AgentResponse();
                var testId = "test-id-123";
                var testName = "Test Agent";

                // Act
                response.Id = testId;
                response.Name = testName;

                // Assert
                Assert.That(response.Id, Is.EqualTo(testId));
                Assert.That(response.Name, Is.EqualTo(testName));
            }
        }

        [TestFixture]
        public class MessageRequestTests
        {
            [Test]
            public void DefaultConstructor_SetsEmptyText()
            {
                // Act
                var request = new MessageRequest();

                // Assert
                Assert.That(request.Text, Is.EqualTo(string.Empty));
                Assert.That(request.ThreadId, Is.Null);
            }

            [Test]
            public void Properties_CanBeSet()
            {
                // Arrange
                var request = new MessageRequest();
                var testText = "Hello, world!";
                var testThreadId = "thread-123";

                // Act
                request.Text = testText;
                request.ThreadId = testThreadId;

                // Assert
                Assert.That(request.Text, Is.EqualTo(testText));
                Assert.That(request.ThreadId, Is.EqualTo(testThreadId));
            }

            [Test]
            public void ThreadId_CanBeNull()
            {
                // Arrange
                var request = new MessageRequest();

                // Act
                request.ThreadId = null;

                // Assert
                Assert.That(request.ThreadId, Is.Null);
            }
        }

        [TestFixture]
        public class MessageResponseTests
        {
            [Test]
            public void DefaultConstructor_SetsEmptyValues()
            {
                // Act
                var response = new MessageResponse();

                // Assert
                Assert.That(response.AgentId, Is.EqualTo(string.Empty));
                Assert.That(response.ThreadId, Is.EqualTo(string.Empty));
                Assert.That(response.Text, Is.EqualTo(string.Empty));
            }

            [Test]
            public void Properties_CanBeSet()
            {
                // Arrange
                var response = new MessageResponse();
                var testAgentId = "agent-123";
                var testThreadId = "thread-456";
                var testText = "Hello, human!";

                // Act
                response.AgentId = testAgentId;
                response.ThreadId = testThreadId;
                response.Text = testText;

                // Assert
                Assert.That(response.AgentId, Is.EqualTo(testAgentId));
                Assert.That(response.ThreadId, Is.EqualTo(testThreadId));
                Assert.That(response.Text, Is.EqualTo(testText));
            }
        }

        [TestFixture]
        public class ChatMessageTests
        {
            [Test]
            public void DefaultConstructor_SetsDefaultValues()
            {
                // Act
                var chatMessage = new ChatMessage();

                // Assert
                Assert.That(chatMessage.Text, Is.EqualTo(string.Empty));
                Assert.That(chatMessage.IsUser, Is.False);
                Assert.That(chatMessage.Timestamp, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
            }

            [Test]
            public void Properties_CanBeSet()
            {
                // Arrange
                var chatMessage = new ChatMessage();
                var testText = "Test message";
                var testTimestamp = new DateTime(2023, 1, 1, 12, 0, 0);

                // Act
                chatMessage.Text = testText;
                chatMessage.IsUser = true;
                chatMessage.Timestamp = testTimestamp;

                // Assert
                Assert.That(chatMessage.Text, Is.EqualTo(testText));
                Assert.That(chatMessage.IsUser, Is.True);
                Assert.That(chatMessage.Timestamp, Is.EqualTo(testTimestamp));
            }

            [Test]
            public void DisplayTime_ReturnsFormattedTime()
            {
                // Arrange
                var chatMessage = new ChatMessage
                {
                    Timestamp = new DateTime(2023, 1, 1, 14, 30, 45)
                };

                // Act
                var displayTime = chatMessage.DisplayTime;

                // Assert
                Assert.That(displayTime, Is.EqualTo("14:30:45"));
            }

            [Test]
            [TestCase(true)]
            [TestCase(false)]
            public void IsUser_Property_WorksCorrectly(bool isUser)
            {
                // Arrange
                var chatMessage = new ChatMessage();

                // Act
                chatMessage.IsUser = isUser;

                // Assert
                Assert.That(chatMessage.IsUser, Is.EqualTo(isUser));
            }

            [Test]
            public void DisplayTime_WithDifferentTimes_ReturnsCorrectFormat()
            {
                // Test cases for different times
                var testCases = new[]
                {
                    (new DateTime(2023, 1, 1, 0, 0, 0), "00:00:00"),
                    (new DateTime(2023, 1, 1, 9, 5, 3), "09:05:03"),
                    (new DateTime(2023, 1, 1, 23, 59, 59), "23:59:59"),
                    (new DateTime(2023, 1, 1, 12, 0, 0), "12:00:00")
                };

                foreach (var (timestamp, expectedDisplay) in testCases)
                {
                    // Arrange
                    var chatMessage = new ChatMessage { Timestamp = timestamp };

                    // Act
                    var displayTime = chatMessage.DisplayTime;

                    // Assert
                    Assert.That(displayTime, Is.EqualTo(expectedDisplay), 
                        $"Failed for timestamp {timestamp}");
                }
            }
        }
    }
}