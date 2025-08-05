using LombdaAgentAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace LombdaAgentAPI.Middleware
{
    /// <summary>
    /// Authentication handler for API key authentication
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IApiTokenService _tokenService;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IServiceProvider serviceProvider) : base(options, logger, encoder)
        {
            _tokenService = serviceProvider.GetRequiredService<IApiTokenService>();
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // This is a minimal handler - the actual authentication is done by the middleware
            // This handler exists to satisfy ASP.NET Core's authentication requirements
            return AuthenticateResult.NoResult();
        }
    }

    /// <summary>
    /// Middleware to handle API key authentication
    /// </summary>
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IApiTokenService tokenService)
        {
            // Skip authentication for certain paths
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (ShouldSkipAuthentication(path))
            {
                await _next(context);
                return;
            }

            // Try to get API key from various sources
            var apiKey = GetApiKeyFromRequest(context);

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API key is required");
                return;
            }

            try
            {
                // Validate the API key
                var account = await tokenService.ValidateTokenAsync(apiKey);
                
                if (account == null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid API key");
                    return;
                }

                // Set up the user context
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, account.Id),
                    new Claim(ClaimTypes.Name, account.Username),
                    new Claim(ClaimTypes.Email, account.Email),
                    new Claim("DisplayName", account.DisplayName)
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                // Add account information to the context for easy access
                context.Items["Account"] = account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error");
                return;
            }

            await _next(context);
        }

        private static bool ShouldSkipAuthentication(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Skip authentication for these paths
            var publicPaths = new[]
            {
                "/swagger",
                "/health",
                "/v1/accounts", // Allow account creation and token creation for testing
                "/"
            };

            return publicPaths.Any(publicPath => path.StartsWith(publicPath));
        }

        private static string? GetApiKeyFromRequest(HttpContext context)
        {
            // Try Authorization header first (Bearer format)
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring(7);
            }

            // Try custom header
            var apiKeyHeader = context.Request.Headers["X-API-Key"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKeyHeader))
            {
                return apiKeyHeader;
            }

            // Try query parameter (not recommended for production)
            var queryParam = context.Request.Query["api_key"].FirstOrDefault();
            if (!string.IsNullOrEmpty(queryParam))
            {
                return queryParam;
            }

            return null;
        }
    }

    /// <summary>
    /// Extension methods for adding API key authentication middleware
    /// </summary>
    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        /// <summary>
        /// Adds API key authentication middleware to the application pipeline
        /// </summary>
        /// <param name="builder">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }
    }
}