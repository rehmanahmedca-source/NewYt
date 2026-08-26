using SQLite;

namespace YTDownloaderXPro.Models;

public class DatabaseService
{
    private SQLiteConnection? _db;
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "database.db");
    }

    public SQLiteConnection GetConnection()
    {
        if (_db == null)
        {
            _db = new SQLiteConnection(_dbPath);
        }
        return _db;
    }

    public void Initialize()
    {
        var db = GetConnection();
        db.CreateTable<DownloadTask>();
        db.CreateTable<HistoryEntry>();
        db.CreateTable<SettingsRecord>();
    }
}

[Table("downloads")]
public class DownloadTask
{
    [PrimaryKey, MaxLength(16)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    [MaxLength(512)]
    public string Url { get; set; } = "";
    [MaxLength(512)]
    public string Title { get; set; } = "Untitled";
    [MaxLength(512)]
    public string Thumbnail { get; set; } = "";
    [MaxLength(256)]
    public string Uploader { get; set; } = "";
    [MaxLength(64)]
    public string FormatId { get; set; } = "best";
    [MaxLength(64)]
    public string QualityLabel { get; set; } = "Best";
    [MaxLength(32)]
    public string Status { get; set; } = "queued";
    public double Speed { get; set; }
    public int EtaSeconds { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double Progress { get; set; }
    [MaxLength(1024)]
    public string OutputFile { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public int RetryCount { get; set; }
    public int Priority { get; set; }
    [MaxLength(32)]
    public string SessionId { get; set; } = "";
    public double CreatedTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    public double? FinishedTime { get; set; }
}

[Table("history")]
public class HistoryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [MaxLength(512)]
    public string Title { get; set; } = "Untitled";
    [MaxLength(512)]
    public string Thumbnail { get; set; } = "";
    [MaxLength(256)]
    public string Uploader { get; set; } = "";
    public double DateCompleted { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    public int DurationSeconds { get; set; }
    [MaxLength(64)]
    public string FormatId { get; set; } = "";
    [MaxLength(64)]
    public string QualityLabel { get; set; } = "";
    public long SizeBytes { get; set; }
    [MaxLength(1024)]
    public string OutputPath { get; set; } = "";
    [MaxLength(32)]
    public string Status { get; set; } = "completed";
    [MaxLength(32)]
    public string SessionId { get; set; } = "";
}

[Table("settings")]
public class SettingsRecord
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    [MaxLength(1024)]
    public string DownloadFolder { get; set; } = "";
    [MaxLength(1024)]
    public string TempFolder { get; set; } = "";
    [MaxLength(256)]
    public string FilenameTemplate { get; set; } = "%(title)s.%(ext)s";
    public int MaxConcurrent { get; set; } = 3;
    public int ConcurrentFragments { get; set; } = 8;
    public int MaxRetries { get; set; } = 3;
    public bool AutoResume { get; set; } = true;
    public int SpeedLimitKbps { get; set; }
    [MaxLength(32)]
    public string DefaultQuality { get; set; } = "best";
    [MaxLength(32)]
    public string Theme { get; set; } = "dark";
    [MaxLength(16)]
    public string Language { get; set; } = "en";
    [MaxLength(256)]
    public string Proxy { get; set; } = "";
    [MaxLength(1024)]
    public string CookiesPath { get; set; } = "";
    [MaxLength(1024)]
    public string FfmpegPath { get; set; } = "";
    public bool EmbedMetadata { get; set; }
    public bool EmbedThumbnail { get; set; }
    public bool EmbedSubtitles { get; set; }
    public bool Sponsorblock { get; set; }
}
