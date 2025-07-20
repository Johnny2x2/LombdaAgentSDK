using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Hubs;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using Microsoft.AspNetCore.ResponseCompression;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args);
    }

    public static void CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR();

        // Add response compression for SSE
        builder.Services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "text/event-stream" });
        });

        // Register the LombdaAgentService as a singleton
        builder.Services.AddSingleton<ILombdaAgentService, LombdaAgentService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<AgentHub>("/agentHub");

        app.Run();
    }
}
