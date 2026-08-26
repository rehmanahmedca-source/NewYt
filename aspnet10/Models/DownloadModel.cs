namespace YTDownloaderXPro.Models;

public class DownloadTask
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "Untitled";
    public string Thumbnail { get; set; } = "";
    public string Uploader { get; set; } = "";
    public string FormatId { get; set; } = "best";
    public string QualityLabel { get; set; } = "Best";
    public string Status { get; set; } = "queued";
    public double Speed { get; set; }
    public int EtaSeconds { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double Progress { get; set; }
    public string OutputFile { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public int RetryCount { get; set; }
    public int Priority { get; set; }
    public string SessionId { get; set; } = "";
    public double CreatedTime { get; set; }
    public double? FinishedTime { get; set; }

    public Dictionary<string, object?> ToDict() => new()
    {
        ["id"] = Id, ["url"] = Url, ["title"] = Title,
        ["thumbnail"] = Thumbnail, ["uploader"] = Uploader,
        ["format_id"] = FormatId, ["quality_label"] = QualityLabel,
        ["status"] = Status, ["speed"] = Speed,
        ["eta_seconds"] = EtaSeconds, ["downloaded_bytes"] = DownloadedBytes,
        ["total_bytes"] = TotalBytes, ["progress"] = Math.Round(Progress, 1),
        ["output_file"] = OutputFile, ["error_message"] = ErrorMessage,
        ["retry_count"] = RetryCount, ["priority"] = Priority,
        ["created_time"] = CreatedTime, ["finished_time"] = FinishedTime
    };
}

public class HistoryEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Thumbnail { get; set; } = "";
    public string Uploader { get; set; } = "";
    public double DateCompleted { get; set; }
    public int DurationSeconds { get; set; }
    public string FormatId { get; set; } = "";
    public string QualityLabel { get; set; } = "";
    public long SizeBytes { get; set; }
    public string OutputPath { get; set; } = "";
    public string Status { get; set; } = "completed";
    public string SessionId { get; set; } = "";

    public Dictionary<string, object?> ToDict() => new()
    {
        ["id"] = Id, ["title"] = Title, ["thumbnail"] = Thumbnail,
        ["uploader"] = Uploader, ["date_completed"] = DateCompleted,
        ["duration_seconds"] = DurationSeconds, ["format_id"] = FormatId,
        ["quality_label"] = QualityLabel, ["size_bytes"] = SizeBytes,
        ["output_path"] = OutputPath, ["status"] = Status
    };
}

public class SettingsRecord
{
    public int Id { get; set; } = 1;
    public string DownloadFolder { get; set; } = "";
    public string TempFolder { get; set; } = "";
    public string FilenameTemplate { get; set; } = "%(title)s.%(ext)s";
    public int MaxConcurrent { get; set; } = 3;
    public int ConcurrentFragments { get; set; } = 8;
    public int MaxRetries { get; set; } = 3;
    public bool AutoResume { get; set; } = true;
    public int SpeedLimitKbps { get; set; }
    public string DefaultQuality { get; set; } = "best";
    public string Theme { get; set; } = "dark";
    public string Language { get; set; } = "en";
    public string Proxy { get; set; } = "";
    public string CookiesPath { get; set; } = "";
    public string FfmpegPath { get; set; } = "";
    public bool EmbedMetadata { get; set; }
    public bool EmbedThumbnail { get; set; }
    public bool EmbedSubtitles { get; set; }
    public bool Sponsorblock { get; set; }
}
