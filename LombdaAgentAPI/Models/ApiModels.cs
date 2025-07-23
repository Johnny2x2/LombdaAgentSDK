namespace LombdaAgentAPI.Models
{
    /// <summary>
    /// Request to create a new agent
    /// </summary>
    public class AgentCreationRequest
    {
        /// <summary>
        /// Name for the new agent
        /// </summary>
        public string Name { get; set; } = "Assistant";
        /// <summary>
        /// Gets or sets the type of agent.
        /// </summary>
        public string AgentType { get; set; } = "Default";
    }

    /// <summary>
    /// Response with agent details
    /// </summary>
    public class AgentResponse
    {
        /// <summary>
        /// Agent ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Agent name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to send a message to an agent
    /// </summary>
    public class MessageRequest
    {
        /// <summary>
        /// Message text content
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Optional thread ID for conversation context
        /// </summary>
        public string? ThreadId { get; set; }
    }

    /// <summary>
    /// Response from agent message
    /// </summary>
    public class MessageResponse
    {
        /// <summary>
        /// Agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Thread ID for this conversation
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;

        /// <summary>
        /// Response text
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}