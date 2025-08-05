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

        /// <summary>
        /// Gets or sets the file data encoded in Base64 format in URL format.
        /// </summary>
        public string? FileBase64Data{ get; set; } = string.Empty;
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

    // Account and API Token Models

    /// <summary>
    /// Request to create a new account
    /// </summary>
    public class CreateAccountRequest
    {
        /// <summary>
        /// Username for the account
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address for the account
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the account
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response containing account information
    /// </summary>
    public class AccountResponse
    {
        /// <summary>
        /// Account ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Account creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Whether the account is active
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Request to create a new API token
    /// </summary>
    public class CreateApiTokenRequest
    {
        /// <summary>
        /// Name/description for the API token
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional expiration date for the token
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Response containing API token information
    /// </summary>
    public class ApiTokenResponse
    {
        /// <summary>
        /// Token ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Token name/description
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The actual API token (only returned on creation)
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Token creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Token expiration date
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Last time the token was used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Whether the token is active
        /// </summary>
        public bool IsActive { get; set; }
    }
}