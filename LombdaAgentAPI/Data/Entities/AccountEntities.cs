using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LombdaAgentAPI.Data.Entities
{
    /// <summary>
    /// Represents a user account in the system
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Unique username for the account
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address for the account
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the account
        /// </summary>
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// When the account was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the account was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation property for API tokens
        /// </summary>
        public virtual ICollection<ApiToken> ApiTokens { get; set; } = new List<ApiToken>();
    }

    /// <summary>
    /// Represents an API token for accessing the API
    /// </summary>
    public class ApiToken
    {
        /// <summary>
        /// Unique identifier for the token
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name/description for the token
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The hashed token value
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Prefix of the token for identification (first 8 characters)
        /// </summary>
        [MaxLength(12)]
        public string TokenPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Account ID that owns this token
        /// </summary>
        [Required]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// When the token was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional expiration date for the token
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Last time the token was used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Whether the token is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation property for the account that owns this token
        /// </summary>
        [ForeignKey("AccountId")]
        public virtual Account Account { get; set; } = null!;
    }
}