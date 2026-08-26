using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using YTDownloaderXPro.Models;
using YTDownloaderXPro.Services;
using System.Text.Json;

namespace YTDownloaderXPro.Controllers;

[Route("api")]
[ApiController]
public class ApiController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly DownloaderService _downloader;
    private readonly QueueManagerService _queue;
    private readonly SettingsService _settings;
    private readonly HistoryService _history;

    public ApiController(DatabaseService db, DownloaderService downloader, QueueManagerService queue, SettingsService settings, HistoryService history)
    {
        _db = db; _downloader = downloader; _queue = queue; _settings = settings; _history = history;
    }

    private string GetSessionId()
    {
        if (HttpContext.Session.GetString("sid") == null)
            HttpContext.Session.SetString("sid", Guid.NewGuid().ToString("N")[..16]);
        return HttpContext.Session.GetString("sid")!;
    }

    private bool IsValidYoutubeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != "http" && uri.Scheme != "https") return false;
        var host = uri.Host.ToLower();
        return host.Contains("youtube.com") || host.Contains("youtu.be");
    }

    [HttpPost("fetch")]
    public IActionResult Fetch([FromBody] JsonElement body)
    {
        var url = body.GetProperty("url").GetString()?.Trim() ?? "";
        if (!IsValidYoutubeUrl(url)) return BadRequest(new { error = "Invalid YouTube URL" });
        try
        {
            var result = _downloader.FetchOverview(url);
            return Ok(result.RootElement);
        }
        catch (Exception ex) { return StatusCode(502, new { error = ex.Message }); }
    }

    [HttpPost("queue/add")]
    public IActionResult QueueAdd([FromBody] JsonElement body)
    {
        var sid = GetSessionId();
        var items = body.TryGetProperty("items", out var arr) ? arr.EnumerateArray().Select(e => e).ToList() : new List<JsonElement> { body };
        var added = new List<string>();
        foreach (var item in items)
        {
            var url = item.GetProperty("url").GetString()?.Trim() ?? "";
            if (!IsValidYoutubeUrl(url)) continue;
            var task = _queue.AddTask(url,
                item.TryGetProperty("format_id", out var fi) ? fi.GetString() ?? "best" : "bestvideo+bestaudio/best",
                item.TryGetProperty("quality_label", out var ql) ? ql.GetString() ?? "Best" : "Best",
                item.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled",
                item.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? "" : "",
                item.TryGetProperty("uploader", out var u) ? u.GetString() ?? "" : "",
                sid);
            added.Add(task.Id);
        }
        return Ok(new { added, count = added.Count });
    }

    [HttpGet("tasks")]
    public IActionResult Tasks() => Ok(_queue.GetTasksBySession(GetSessionId()));

    [HttpPost("tasks/{taskId}/{action}")]
    public IActionResult TaskAction(string taskId, string action)
    {
        var ok = action switch
        {
            "pause" => _queue.Pause(taskId),
            "resume" => _queue.Resume(taskId),
            "cancel" => _queue.Cancel(taskId),
            "retry" => _queue.Retry(taskId),
            "remove" => _queue.Remove(taskId),
            _ => false
        };
        return Ok(new { ok });
    }

    [HttpGet("history")]
    public IActionResult HistoryList([FromQuery] string search = "")
    {
        var entries = _history.List(search);
        return Ok(entries.Select(e => e.ToDict()));
    }

    [HttpDelete("history/{id}")]
    public IActionResult HistoryDelete(int id) => Ok(new { ok = _history.Remove(id) });

    [HttpGet("history/export.csv")]
    public IActionResult HistoryExport()
    {
        var entries = _history.List();
        var csv = "Title,Uploader,Date,Quality,Size,Status,Path\n";
        foreach (var e in entries)
            csv += $"\"{e.Title}\",\"{e.Uploader}\",{e.DateCompleted},\"{e.QualityLabel}\",{e.SizeBytes},\"{e.Status}\",\"{e.OutputPath}\"\n";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "history.csv");
    }

    [HttpGet("settings")]
    public IActionResult SettingsGet() => Ok(_settings.GetSettings());

    [HttpPost("settings")]
    public IActionResult SettingsPost([FromBody] Dictionary<string, object> data) => Ok(_settings.UpdateSettings(data));

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        var sid = GetSessionId();
        var completed = _queue.CountBySessionAndStatus(sid, "completed");
        var failed = _queue.CountBySessionAndStatus(sid, "failed");
        var active = _queue.CountBySessionAndStatus(sid, "downloading");
        var queued = _queue.CountBySessionAndStatus(sid, "queued");
        var bytes = _queue.SumBytesBySessionAndStatus(sid, "completed");
        var attempts = completed + failed;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var labels = new List<string>();
        var counts = new List<int>();
        for (int i = 6; i >= 0; i--)
        {
            var dayStart = now - i * 86400;
            labels.Add(DateTimeOffset.FromUnixTimeSeconds(dayStart).ToString("ddd"));
            counts.Add(_history.CountByDayRange(dayStart, dayStart + 86400));
        }
        var settings = _settings.GetSettings();
        var driveInfo = new DriveInfo(Path.GetPathRoot(settings.DownloadFolder) ?? "/");
        return Ok(new
        {
            total_downloads = _history.CountAll(),
            completed_downloads = completed, failed_downloads = failed,
            active_downloads = active, queued_downloads = queued,
            success_rate = Math.Round(attempts > 0 ? (double)completed / attempts * 100 : 0, 1),
            downloaded_session_bytes = bytes,
            total_gb_downloaded = Math.Round((double)_history.SumSizeBytes() / (1024.0 * 1024 * 1024), 2),
            disk_used_bytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
            disk_total_bytes = driveInfo.TotalSize,
            disk_free_bytes = driveInfo.AvailableFreeSpace,
            chart_days_labels = labels, chart_days_counts = counts,
            top_uploader = "-", top_format = "-"
        });
    }
}
