using System.Text;
using System.Text.Json;
using LombdaAgentMAUI.Core.Models;

namespace LombdaAgentMAUI.Tests.Helpers
{
    public static class TestDataFactory
    {
        private static readonly Random _random = new();

        public static AgentResponse CreateAgentResponse(string? id = null, string? name = null)
        {
            return new AgentResponse
            {
                Id = id ?? $"agent-{Guid.NewGuid()}",
                Name = name ?? $"Test Agent {_random.Next(1000)}"
            };
        }

        public static AgentCreationRequest CreateAgentCreationRequest(string? name = null)
        {
            return new AgentCreationRequest
            {
                Name = name ?? $"Test Agent {_random.Next(1000)}"
            };
        }

        public static MessageRequest CreateMessageRequest(string? text = null, string? threadId = null)
        {
            return new MessageRequest
            {
                Text = text ?? $"Test message {_random.Next(1000)}",
                ThreadId = threadId
            };
        }

        public static MessageResponse CreateMessageResponse(string? agentId = null, string? threadId = null, string? text = null)
        {
            return new MessageResponse
            {
                AgentId = agentId ?? $"agent-{Guid.NewGuid()}",
                ThreadId = threadId ?? $"thread-{Guid.NewGuid()}",
                Text = text ?? $"Test response {_random.Next(1000)}"
            };
        }

        public static ChatMessage CreateChatMessage(string? text = null, bool? isUser = null, DateTime? timestamp = null)
        {
            return new ChatMessage
            {
                Text = text ?? $"Test chat message {_random.Next(1000)}",
                IsUser = isUser ?? _random.Next(2) == 0,
                Timestamp = timestamp ?? DateTime.Now
            };
        }

        public static List<string> CreateAgentIdList(int count = 3)
        {
            var agents = new List<string>();
            for (int i = 0; i < count; i++)
            {
                agents.Add($"agent-{Guid.NewGuid()}");
            }
            return agents;
        }

        public static List<ChatMessage> CreateChatMessageList(int count = 5)
        {
            var messages = new List<ChatMessage>();
            for (int i = 0; i < count; i++)
            {
                messages.Add(CreateChatMessage(
                    text: $"Message {i + 1}",
                    isUser: i % 2 == 0, // Alternate between user and agent messages
                    timestamp: DateTime.Now.AddMinutes(-count + i)
                ));
            }
            return messages;
        }

        public static string SerializeToJson<T>(T obj)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Serialize(obj, options);
        }

        public static StringContent CreateJsonContent<T>(T obj)
        {
            var json = SerializeToJson(obj);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }

    public static class TestConstants
    {
        public const string DefaultApiUrl = "https://localhost:5001/";
        public const string TestApiUrl = "https://test.example.com/";
        public const string InvalidApiUrl = "not-a-valid-url";
        
        public const string TestAgentId = "test-agent-123";
        public const string TestAgentName = "Test Agent";
        public const string TestThreadId = "test-thread-456";
        public const string TestMessage = "Hello, this is a test message";
        public const string TestResponse = "Hello! How can I help you today?";

        public static readonly DateTime TestTimestamp = new(2023, 1, 1, 12, 0, 0);
    }

    public static class ApiEndpoints
    {
        public const string Agents = "v1/agents";
        public static string GetAgent(string id) => $"v1/agents/{id}";
        public static string SendMessage(string id) => $"v1/agents/{id}/messages";
        public static string SendMessageStream(string id) => $"v1/agents/{id}/messages/stream";
    }
}