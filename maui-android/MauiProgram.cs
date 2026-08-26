using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<DownloaderService>();
        builder.Services.AddSingleton<QueueManagerService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<HistoryService>();

        // Register pages
        builder.Services.AddTransient<Views.DashboardPage>();
        builder.Services.AddTransient<Views.DownloadsPage>();
        builder.Services.AddTransient<Views.HistoryPage>();
        builder.Services.AddTransient<Views.SettingsPage>();
        builder.Services.AddTransient<Views.AboutPage>();

        // Register ViewModels
        builder.Services.AddTransient<ViewModels.DashboardViewModel>();
        builder.Services.AddTransient<ViewModels.DownloadsViewModel>();
        builder.Services.AddTransient<ViewModels.HistoryViewModel>();
        builder.Services.AddTransient<ViewModels.SettingsViewModel>();

        return builder.Build();
    }
}
