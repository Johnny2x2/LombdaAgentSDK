using LombdaAgentAPI.Models;
using LombdaAgentAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LombdaAgentAPI.Controllers
{
    /// <summary>
    /// Controller for managing user accounts
    /// </summary>
    [ApiController]
    [Route("v1/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IApiTokenService _tokenService;

        public AccountsController(IAccountService accountService, IApiTokenService tokenService)
        {
            _accountService = accountService;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        /// <param name="request">Account creation request</param>
        /// <returns>Created account information</returns>
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] CreateAccountRequest request)
        {
            try
            {
                var account = await _accountService.CreateAccountAsync(request);
                return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating account: {ex.Message}");
            }
        }

        /// <summary>
        /// Get account information (requires authentication)
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <returns>Account information</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponse>> GetAccount(string id)
        {
            // Users can only access their own account unless they're admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != id)
            {
                return StatusCode(403, "You can only access your own account");
            }

            var account = await _accountService.GetAccountAsync(id);
            if (account == null)
                return NotFound();

            return Ok(account);
        }

        /// <summary>
        /// Get current user's account information
        /// </summary>
        /// <returns>Account information</returns>
        [HttpGet("me")]
        public async Task<ActionResult<AccountResponse>> GetCurrentAccount()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var account = await _accountService.GetAccountAsync(currentUserId);
            if (account == null)
                return NotFound();

            return Ok(account);
        }

        /// <summary>
        /// Update account information
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated account information</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<AccountResponse>> UpdateAccount(string id, [FromBody] CreateAccountRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != id)
            {
                return StatusCode(403, "You can only update your own account");
            }

            try
            {
                var account = await _accountService.UpdateAccountAsync(id, request);
                if (account == null)
                    return NotFound();

                return Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating account: {ex.Message}");
            }
        }

        /// <summary>
        /// Deactivate account
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeactivateAccount(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != id)
            {
                return StatusCode(403, "You can only deactivate your own account");
            }

            var success = await _accountService.DeactivateAccountAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // API Token endpoints

        /// <summary>
        /// Create a new API token for a specific account (Testing endpoint - no auth required)
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Token creation request</param>
        /// <returns>Created token information</returns>
        [HttpPost("{accountId}/tokens")]
        public async Task<ActionResult<ApiTokenResponse>> CreateApiTokenForAccount(string accountId, [FromBody] CreateApiTokenRequest request)
        {
            try
            {
                var token = await _tokenService.CreateTokenAsync(accountId, request);
                return Ok(token);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating token: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new API token for the current account
        /// </summary>
        /// <param name="request">Token creation request</param>
        /// <returns>Created token information</returns>
        [HttpPost("me/tokens")]
        public async Task<ActionResult<ApiTokenResponse>> CreateApiToken([FromBody] CreateApiTokenRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            try
            {
                var token = await _tokenService.CreateTokenAsync(currentUserId, request);
                return Ok(token);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating token: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all API tokens for the current account
        /// </summary>
        /// <returns>List of tokens (without actual token values)</returns>
        [HttpGet("me/tokens")]
        public async Task<ActionResult<List<ApiTokenResponse>>> GetApiTokens()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var tokens = await _tokenService.GetTokensForAccountAsync(currentUserId);
            return Ok(tokens);
        }

        /// <summary>
        /// Revoke an API token
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("me/tokens/{tokenId}")]
        public async Task<ActionResult> RevokeApiToken(string tokenId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var success = await _tokenService.RevokeTokenAsync(currentUserId, tokenId);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}