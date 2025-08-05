using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Data;
using LombdaAgentAPI.Hubs;
using LombdaAgentAPI.Middleware;
using LombdaAgentAPI.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args);
    }

    public static async Task CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Add Authentication services (required for Forbid() to work)
        builder.Services.AddAuthentication("ApiKey")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                "ApiKey", options => { });

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "LombdaAgent API", Version = "v1" });
            
            // Add API Key authentication to Swagger
            c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "X-API-Key",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Description = "API Key needed to access the endpoints"
            });

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "API Key",
                Description = "API Key in Authorization header using Bearer scheme"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        builder.Services.AddSignalR();

        // Add response compression for SSE
        builder.Services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "text/event-stream" });
        });

        // Add database context
        builder.Services.AddDbContext<LombdaAgentDbContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=lombdaagent.db");
        });

        // Register services
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IApiTokenService, ApiTokenService>();
        builder.Services.AddSingleton<ILombdaAgentService, LombdaAgentService>();

        var app = builder.Build();

        // Initialize database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LombdaAgentDbContext>();
            await context.InitializeAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();

        // Add API key authentication middleware FIRST (before ASP.NET Core auth)
        app.UseApiKeyAuthentication();

        // Add authentication middleware (order matters!)
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<AgentHub>("/agentHub");

        await app.RunAsync();
    }
}
