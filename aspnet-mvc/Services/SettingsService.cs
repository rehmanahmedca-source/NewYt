using Microsoft.Data.Sqlite;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.Services;

public class SettingsService
{
    private readonly DatabaseService _db;
    private readonly IConfiguration _config;

    public SettingsService(DatabaseService db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public void EnsureDefaults()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM settings WHERE id = 1";
        var count = (long)cmd.ExecuteScalar()!;
        if (count == 0)
        {
            var appConfig = _config.GetSection("AppConfig");
            cmd.CommandText = @"INSERT INTO settings (id, download_folder, temp_folder, max_concurrent, concurrent_fragments, max_retries)
                               VALUES (1, @df, @tf, @mc, @cf, @mr)";
            cmd.Parameters.AddWithValue("@df", Path.Combine(Directory.GetCurrentDirectory(), appConfig["DefaultDownloadFolder"] ?? "downloads"));
            cmd.Parameters.AddWithValue("@tf", Path.Combine(Directory.GetCurrentDirectory(), appConfig["DefaultTempFolder"] ?? "temp"));
            cmd.Parameters.AddWithValue("@mc", int.Parse(appConfig["DefaultMaxConcurrent"] ?? "3"));
            cmd.Parameters.AddWithValue("@cf", int.Parse(appConfig["DefaultConcurrentFragments"] ?? "8"));
            cmd.Parameters.AddWithValue("@mr", int.Parse(appConfig["DefaultMaxRetries"] ?? "3"));
            cmd.ExecuteNonQuery();
        }
    }

    public SettingsRecord GetSettings()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM settings WHERE id = 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new SettingsRecord
            {
                DownloadFolder = reader.GetString(reader.GetOrdinal("download_folder")),
                TempFolder = reader.GetString(reader.GetOrdinal("temp_folder")),
                FilenameTemplate = reader.GetString(reader.GetOrdinal("filename_template")),
                MaxConcurrent = reader.GetInt32(reader.GetOrdinal("max_concurrent")),
                ConcurrentFragments = reader.GetInt32(reader.GetOrdinal("concurrent_fragments")),
                MaxRetries = reader.GetInt32(reader.GetOrdinal("max_retries")),
                AutoResume = reader.GetInt32(reader.GetOrdinal("auto_resume")) == 1,
                SpeedLimitKbps = reader.GetInt32(reader.GetOrdinal("speed_limit_kbps")),
                DefaultQuality = reader.GetString(reader.GetOrdinal("default_quality")),
                Theme = reader.GetString(reader.GetOrdinal("theme")),
                Language = reader.GetString(reader.GetOrdinal("language")),
                Proxy = reader.GetString(reader.GetOrdinal("proxy")),
                CookiesPath = reader.GetString(reader.GetOrdinal("cookies_path")),
                FfmpegPath = reader.GetString(reader.GetOrdinal("ffmpeg_path")),
                EmbedMetadata = reader.GetInt32(reader.GetOrdinal("embed_metadata")) == 1,
                EmbedThumbnail = reader.GetInt32(reader.GetOrdinal("embed_thumbnail")) == 1,
                EmbedSubtitles = reader.GetInt32(reader.GetOrdinal("embed_subtitles")) == 1,
                Sponsorblock = reader.GetInt32(reader.GetOrdinal("sponsorblock")) == 1
            };
        }
        return new SettingsRecord();
    }

    public SettingsRecord UpdateSettings(Dictionary<string, object> data)
    {
        using var conn = _db.GetConnection();
        var sets = new List<string>();
        var cmd = conn.CreateCommand();
        int i = 0;
        foreach (var kv in data)
        {
            sets.Add($"{kv.Key} = @p{i}");
            cmd.Parameters.AddWithValue($"@p{i}", kv.Value?.ToString() ?? "");
            i++;
        }
        if (sets.Count > 0)
        {
            cmd.CommandText = $"UPDATE settings SET {string.Join(", ", sets)} WHERE id = 1";
            cmd.ExecuteNonQuery();
        }
        return GetSettings();
    }
}
