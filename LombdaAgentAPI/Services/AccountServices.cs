using LombdaAgentAPI.Data;
using LombdaAgentAPI.Data.Entities;
using LombdaAgentAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LombdaAgentAPI.Services
{
    /// <summary>
    /// Service for managing user accounts
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly LombdaAgentDbContext _context;

        public AccountService(LombdaAgentDbContext context)
        {
            _context = context;
        }

        public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request)
        {
            // Check if username or email already exists
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == request.Username || a.Email == request.Email);

            if (existingAccount != null)
            {
                throw new InvalidOperationException("Username or email already exists");
            }

            var account = new Account
            {
                Username = request.Username,
                Email = request.Email,
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return new AccountResponse
            {
                Id = account.Id,
                Username = account.Username,
                Email = account.Email,
                DisplayName = account.DisplayName,
                CreatedAt = account.CreatedAt,
                IsActive = account.IsActive
            };
        }

        public async Task<AccountResponse?> GetAccountAsync(string accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                return null;

            return new AccountResponse
            {
                Id = account.Id,
                Username = account.Username,
                Email = account.Email,
                DisplayName = account.DisplayName,
                CreatedAt = account.CreatedAt,
                IsActive = account.IsActive
            };
        }

        public async Task<AccountResponse?> GetAccountByUsernameAsync(string username)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
                return null;

            return new AccountResponse
            {
                Id = account.Id,
                Username = account.Username,
                Email = account.Email,
                DisplayName = account.DisplayName,
                CreatedAt = account.CreatedAt,
                IsActive = account.IsActive
            };
        }

        public async Task<AccountResponse?> UpdateAccountAsync(string accountId, CreateAccountRequest request)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                return null;

            // Check if new username or email conflicts with other accounts
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id != accountId && 
                    (a.Username == request.Username || a.Email == request.Email));

            if (existingAccount != null)
            {
                throw new InvalidOperationException("Username or email already exists");
            }

            account.Username = request.Username;
            account.Email = request.Email;
            account.DisplayName = request.DisplayName;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AccountResponse
            {
                Id = account.Id,
                Username = account.Username,
                Email = account.Email,
                DisplayName = account.DisplayName,
                CreatedAt = account.CreatedAt,
                IsActive = account.IsActive
            };
        }

        public async Task<bool> DeactivateAccountAsync(string accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                return false;

            account.IsActive = false;
            account.UpdatedAt = DateTime.UtcNow;

            // Also deactivate all associated tokens
            var tokens = await _context.ApiTokens
                .Where(t => t.AccountId == accountId)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AccountResponse>> GetAllAccountsAsync()
        {
            var accounts = await _context.Accounts
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return accounts.Select(account => new AccountResponse
            {
                Id = account.Id,
                Username = account.Username,
                Email = account.Email,
                DisplayName = account.DisplayName,
                CreatedAt = account.CreatedAt,
                IsActive = account.IsActive
            }).ToList();
        }
    }

    /// <summary>
    /// Service for managing API tokens
    /// </summary>
    public class ApiTokenService : IApiTokenService
    {
        private readonly LombdaAgentDbContext _context;

        public ApiTokenService(LombdaAgentDbContext context)
        {
            _context = context;
        }

        public async Task<ApiTokenResponse> CreateTokenAsync(string accountId, CreateApiTokenRequest request)
        {
            // Verify account exists and is active
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive);

            if (account == null)
            {
                throw new InvalidOperationException("Account not found or inactive");
            }

            // Generate a secure token
            var token = GenerateSecureToken();
            var tokenHash = HashToken(token);
            var tokenPrefix = token.Substring(0, 8);

            var apiToken = new ApiToken
            {
                Name = request.Name,
                TokenHash = tokenHash,
                TokenPrefix = tokenPrefix,
                AccountId = accountId,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.ApiTokens.Add(apiToken);
            await _context.SaveChangesAsync();

            return new ApiTokenResponse
            {
                Id = apiToken.Id,
                Name = apiToken.Name,
                Token = token, // Only return the actual token on creation
                CreatedAt = apiToken.CreatedAt,
                ExpiresAt = apiToken.ExpiresAt,
                LastUsedAt = apiToken.LastUsedAt,
                IsActive = apiToken.IsActive
            };
        }

        public async Task<List<ApiTokenResponse>> GetTokensForAccountAsync(string accountId)
        {
            var tokens = await _context.ApiTokens
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tokens.Select(token => new ApiTokenResponse
            {
                Id = token.Id,
                Name = token.Name,
                Token = null, // Never return the actual token in list operations
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                LastUsedAt = token.LastUsedAt,
                IsActive = token.IsActive
            }).ToList();
        }

        public async Task<AccountResponse?> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHash = HashToken(token);

            var apiToken = await _context.ApiTokens
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                         t.IsActive && 
                                         t.Account.IsActive &&
                                         (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow));

            if (apiToken == null)
                return null;

            // Update last used timestamp in background
            _ = Task.Run(async () => await UpdateTokenLastUsedAsync(tokenHash));

            return new AccountResponse
            {
                Id = apiToken.Account.Id,
                Username = apiToken.Account.Username,
                Email = apiToken.Account.Email,
                DisplayName = apiToken.Account.DisplayName,
                CreatedAt = apiToken.Account.CreatedAt,
                IsActive = apiToken.Account.IsActive
            };
        }

        public async Task<bool> RevokeTokenAsync(string accountId, string tokenId)
        {
            var token = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId && t.AccountId == accountId);

            if (token == null)
                return false;

            token.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateTokenLastUsedAsync(string tokenHash)
        {
            var token = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (token != null)
            {
                token.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ApiTokenResponse?> GetTokenByIdAsync(string tokenId)
        {
            var token = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId);

            if (token == null)
                return null;

            return new ApiTokenResponse
            {
                Id = token.Id,
                Name = token.Name,
                Token = null, // Never return the actual token
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                LastUsedAt = token.LastUsedAt,
                IsActive = token.IsActive
            };
        }

        public async Task RevokeAllTokensForAccountAsync(string accountId)
        {
            var tokens = await _context.ApiTokens
                .Where(t => t.AccountId == accountId && t.IsActive)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateSecureToken()
        {
            // Generate a 32-byte random token and encode as base64url
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            
            // Convert to base64url (URL-safe base64)
            var base64 = Convert.ToBase64String(bytes);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}