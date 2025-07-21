using Microsoft.Extensions.Logging;
using LombdaAgentMAUI.Core.Services;
using LombdaAgentMAUI.Services;

namespace LombdaAgentMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register platform-specific secure storage service
        builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();

        // Register configuration service
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register HTTP client with a factory for dynamic URL
        builder.Services.AddTransient<HttpClient>(serviceProvider =>
        {
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(configService.ApiBaseUrl);
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            return httpClient;
        });

        // Register the API service
        builder.Services.AddTransient<IAgentApiService, AgentApiService>();

        // Register pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
