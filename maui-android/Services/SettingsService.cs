using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.Services;

public class SettingsService
{
    private readonly DatabaseService _db;

    public SettingsService(DatabaseService db) { _db = db; }

    public void EnsureDefaults()
    {
        var db = _db.GetConnection();
        var existing = db.Find<SettingsRecord>(1);
        if (existing == null)
        {
            var downloadFolder = Path.Combine(FileSystem.AppDataDirectory, "downloads");
            Directory.CreateDirectory(downloadFolder);
            db.Insert(new SettingsRecord
            {
                Id = 1,
                DownloadFolder = downloadFolder,
                TempFolder = Path.Combine(FileSystem.AppDataDirectory, "temp")
            });
        }
    }

    public SettingsRecord GetSettings()
    {
        return _db.GetConnection().Find<SettingsRecord>(1) ?? new SettingsRecord();
    }

    public void UpdateSettings(SettingsRecord settings)
    {
        _db.GetConnection().Update(settings);
    }
}
