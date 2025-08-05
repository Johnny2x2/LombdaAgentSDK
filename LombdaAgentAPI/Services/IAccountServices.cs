using LombdaAgentAPI.Data.Entities;
using LombdaAgentAPI.Models;

namespace LombdaAgentAPI.Services
{
    /// <summary>
    /// Interface for account management services
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Creates a new account
        /// </summary>
        /// <param name="request">Account creation request</param>
        /// <returns>Created account response</returns>
        Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request);

        /// <summary>
        /// Gets an account by ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Account response or null if not found</returns>
        Task<AccountResponse?> GetAccountAsync(string accountId);

        /// <summary>
        /// Gets an account by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Account response or null if not found</returns>
        Task<AccountResponse?> GetAccountByUsernameAsync(string username);

        /// <summary>
        /// Updates an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated account response or null if not found</returns>
        Task<AccountResponse?> UpdateAccountAsync(string accountId, CreateAccountRequest request);

        /// <summary>
        /// Deactivates an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if successful</returns>
        Task<bool> DeactivateAccountAsync(string accountId);

        /// <summary>
        /// Gets all accounts (admin function)
        /// </summary>
        /// <returns>List of account responses</returns>
        Task<List<AccountResponse>> GetAllAccountsAsync();
    }

    /// <summary>
    /// Interface for API token management services
    /// </summary>
    public interface IApiTokenService
    {
        /// <summary>
        /// Creates a new API token for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Token creation request</param>
        /// <returns>Created token response with the actual token</returns>
        Task<ApiTokenResponse> CreateTokenAsync(string accountId, CreateApiTokenRequest request);

        /// <summary>
        /// Gets all tokens for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>List of token responses (without actual token values)</returns>
        Task<List<ApiTokenResponse>> GetTokensForAccountAsync(string accountId);

        /// <summary>
        /// Validates an API token and returns the associated account
        /// </summary>
        /// <param name="token">API token</param>
        /// <returns>Account response or null if token is invalid</returns>
        Task<AccountResponse?> ValidateTokenAsync(string token);

        /// <summary>
        /// Revokes an API token
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="tokenId">Token ID</param>
        /// <returns>True if successful</returns>
        Task<bool> RevokeTokenAsync(string accountId, string tokenId);

        /// <summary>
        /// Updates the last used timestamp for a token
        /// </summary>
        /// <param name="tokenHash">Token hash</param>
        /// <returns>Task</returns>
        Task UpdateTokenLastUsedAsync(string tokenHash);

        /// <summary>
        /// Gets the API token by its ID
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Token response or null if not found</returns>
        Task<ApiTokenResponse?> GetTokenByIdAsync(string tokenId);

        /// <summary>
        /// Revokes all API tokens for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Task</returns>
        Task RevokeAllTokensForAccountAsync(string accountId);
    }
}