using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro;

public partial class App : Application
{
    public App(DatabaseService db, SettingsService settings, QueueManagerService queue)
    {
        InitializeComponent();

        // Initialize database
        db.Initialize();
        settings.EnsureDefaults();
        queue.RecoverIncompleteTasks();
        queue.Start();

        MainPage = new AppShell();
    }
}
