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

        // Register session manager service
        builder.Services.AddSingleton<ISessionManagerService, SessionManagerService>();

        // Register the MAUI-specific API service that handles dynamic URLs properly
        builder.Services.AddSingleton<IAgentApiService, MauiAgentApiService>();

        // Register pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
