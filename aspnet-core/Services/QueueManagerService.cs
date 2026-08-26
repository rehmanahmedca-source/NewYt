using Microsoft.Data.Sqlite;
using YTDownloaderXPro.Models;
using System.Diagnostics;

namespace YTDownloaderXPro.Services;

public class QueueManagerService
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;
    private readonly HistoryService _history;
    private readonly ILogger<QueueManagerService> _logger;
    private readonly object _lock = new();
    private int _activeCount;
    private bool _started;

    public QueueManagerService(DatabaseService db, SettingsService settings, HistoryService history, ILogger<QueueManagerService> logger)
    {
        _db = db;
        _settings = settings;
        _history = history;
        _logger = logger;
    }

    public void RecoverIncompleteTasks()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE downloads SET status = 'queued' WHERE status = 'downloading'";
        cmd.ExecuteNonQuery();
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        Task.Run(DispatcherLoop);
    }

    public DownloadTask AddTask(string url, string formatId, string qualityLabel, string title, string thumbnail, string uploader, string sessionId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        var id = Guid.NewGuid().ToString("N")[..12];
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        cmd.CommandText = @"INSERT INTO downloads (id, url, title, thumbnail, uploader, format_id, quality_label, status, session_id, created_time)
                           VALUES (@id, @url, @title, @th, @up, @fi, @ql, 'queued', @si, @ct)";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@url", url);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@th", thumbnail);
        cmd.Parameters.AddWithValue("@up", uploader);
        cmd.Parameters.AddWithValue("@fi", formatId);
        cmd.Parameters.AddWithValue("@ql", qualityLabel);
        cmd.Parameters.AddWithValue("@si", sessionId);
        cmd.Parameters.AddWithValue("@ct", now);
        cmd.ExecuteNonQuery();
        return new DownloadTask { Id = id, Url = url, Title = title, SessionId = sessionId };
    }

    public bool Pause(string taskId) { return UpdateStatus(taskId, "paused"); }
    public bool Resume(string taskId) { return UpdateStatus(taskId, "queued"); }
    public bool Cancel(string taskId) { return UpdateStatus(taskId, "cancelled"); }
    public bool Retry(string taskId) { return UpdateStatus(taskId, "queued"); }

    public bool Remove(string taskId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM downloads WHERE id = @id AND status != 'downloading'";
        cmd.Parameters.AddWithValue("@id", taskId);
        return cmd.ExecuteNonQuery() > 0;
    }

    private bool UpdateStatus(string taskId, string status)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE downloads SET status = @s WHERE id = @id";
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@id", taskId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public List<Dictionary<string, object?>> GetTasksBySession(string sessionId)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM downloads WHERE session_id = @si ORDER BY priority ASC, created_time ASC";
        cmd.Parameters.AddWithValue("@si", sessionId);
        var list = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Dictionary<string, object?>
            {
                ["id"] = reader.GetString(reader.GetOrdinal("id")),
                ["url"] = reader.GetString(reader.GetOrdinal("url")),
                ["title"] = reader.GetString(reader.GetOrdinal("title")),
                ["thumbnail"] = reader.GetString(reader.GetOrdinal("thumbnail")),
                ["uploader"] = reader.GetString(reader.GetOrdinal("uploader")),
                ["format_id"] = reader.GetString(reader.GetOrdinal("format_id")),
                ["quality_label"] = reader.GetString(reader.GetOrdinal("quality_label")),
                ["status"] = reader.GetString(reader.GetOrdinal("status")),
                ["speed"] = reader.GetDouble(reader.GetOrdinal("speed")),
                ["eta_seconds"] = reader.GetInt32(reader.GetOrdinal("eta_seconds")),
                ["downloaded_bytes"] = reader.GetInt64(reader.GetOrdinal("downloaded_bytes")),
                ["total_bytes"] = reader.GetInt64(reader.GetOrdinal("total_bytes")),
                ["progress"] = Math.Round(reader.GetDouble(reader.GetOrdinal("progress")), 1),
                ["output_file"] = reader.GetString(reader.GetOrdinal("output_file")),
                ["error_message"] = reader.GetString(reader.GetOrdinal("error_message")),
                ["retry_count"] = reader.GetInt32(reader.GetOrdinal("retry_count")),
                ["priority"] = reader.GetInt32(reader.GetOrdinal("priority")),
                ["created_time"] = reader.GetDouble(reader.GetOrdinal("created_time")),
                ["finished_time"] = reader.IsDBNull(reader.GetOrdinal("finished_time")) ? null : reader.GetDouble(reader.GetOrdinal("finished_time"))
            });
        }
        return list;
    }

    public int CountBySessionAndStatus(string sessionId, string status)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM downloads WHERE session_id = @si AND status = @st";
        cmd.Parameters.AddWithValue("@si", sessionId);
        cmd.Parameters.AddWithValue("@st", status);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public long SumBytesBySessionAndStatus(string sessionId, string status)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(total_bytes), 0) FROM downloads WHERE session_id = @si AND status = @st";
        cmd.Parameters.AddWithValue("@si", sessionId);
        cmd.Parameters.AddWithValue("@st", status);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private async Task DispatcherLoop()
    {
        while (true)
        {
            try
            {
                var settings = _settings.GetSettings();
                while (_activeCount < settings.MaxConcurrent)
                {
                    using var conn = _db.GetConnection();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id FROM downloads WHERE status = 'queued' ORDER BY priority ASC, created_time ASC LIMIT 1";
                    var taskId = cmd.ExecuteScalar()?.ToString();
                    if (taskId == null) break;

                    lock (_lock) _activeCount++;
                    _ = Task.Run(() => RunDownload(taskId));
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Dispatcher error"); }
            await Task.Delay(800);
        }
    }

    private void RunDownload(string taskId)
    {
        try
        {
            // yt-dlp download implementation would go here
            _logger.LogInformation($"Starting download: {taskId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Download failed: {taskId} -> {ex.Message}");
        }
        finally
        {
            lock (_lock) _activeCount = Math.Max(0, _activeCount - 1);
        }
    }
}
