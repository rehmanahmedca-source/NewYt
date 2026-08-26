using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.Services;

public class QueueManagerService
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;
    private readonly HistoryService _history;
    private readonly DownloaderService _downloader;
    private int _activeCount;
    private bool _started;
    private readonly object _lock = new();
    private readonly Dictionary<string, CancellationTokenSource> _cancellations = new();

    public event Action? QueueChanged;

    public QueueManagerService(DatabaseService db, SettingsService settings, HistoryService history, DownloaderService downloader)
    {
        _db = db; _settings = settings; _history = history; _downloader = downloader;
    }

    public void RecoverIncompleteTasks()
    {
        var db = _db.GetConnection();
        var tasks = db.Table<DownloadTask>().Where(t => t.Status == "downloading").ToList();
        foreach (var t in tasks) { t.Status = "queued"; db.Update(t); }
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        Task.Run(DispatcherLoop);
    }

    public DownloadTask AddTask(string url, string formatId, string qualityLabel, string title, string thumbnail, string uploader)
    {
        var task = new DownloadTask
        {
            Url = url, FormatId = formatId, QualityLabel = qualityLabel,
            Title = title, Thumbnail = thumbnail, Uploader = uploader,
            SessionId = "mobile"
        };
        _db.GetConnection().Insert(task);
        QueueChanged?.Invoke();
        return task;
    }

    public List<DownloadTask> GetTasks()
    {
        return _db.GetConnection().Table<DownloadTask>().OrderBy(t => t.Priority).ThenBy(t => t.CreatedTime).ToList();
    }

    public bool Pause(string taskId)
    {
        var db = _db.GetConnection();
        var task = db.Find<DownloadTask>(taskId);
        if (task == null) return false;
        task.Status = "paused";
        db.Update(task);
        if (_cancellations.TryGetValue(taskId, out var cts)) cts.Cancel();
        QueueChanged?.Invoke();
        return true;
    }

    public bool Resume(string taskId)
    {
        var db = _db.GetConnection();
        var task = db.Find<DownloadTask>(taskId);
        if (task == null || task.Status != "paused") return false;
        task.Status = "queued";
        db.Update(task);
        QueueChanged?.Invoke();
        return true;
    }

    public bool Cancel(string taskId)
    {
        var db = _db.GetConnection();
        var task = db.Find<DownloadTask>(taskId);
        if (task == null) return false;
        task.Status = "cancelled";
        db.Update(task);
        if (_cancellations.TryGetValue(taskId, out var cts)) cts.Cancel();
        QueueChanged?.Invoke();
        return true;
    }

    public bool Remove(string taskId)
    {
        var db = _db.GetConnection();
        var task = db.Find<DownloadTask>(taskId);
        if (task == null || task.Status == "downloading") return false;
        db.Delete(task);
        QueueChanged?.Invoke();
        return true;
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
                    var db = _db.GetConnection();
                    var task = db.Table<DownloadTask>().Where(t => t.Status == "queued").OrderBy(t => t.Priority).ThenBy(t => t.CreatedTime).FirstOrDefault();
                    if (task == null) break;
                    lock (_lock) _activeCount++;
                    task.Status = "downloading";
                    db.Update(task);
                    _ = RunDownload(task.Id);
                }
            }
            catch { }
            await Task.Delay(800);
        }
    }

    private async Task RunDownload(string taskId)
    {
        var cts = new CancellationTokenSource();
        _cancellations[taskId] = cts;
        try
        {
            var db = _db.GetConnection();
            var task = db.Find<DownloadTask>(taskId);
            if (task == null) return;
            var settings = _settings.GetSettings();
            Directory.CreateDirectory(settings.DownloadFolder);

            var outputTemplate = Path.Combine(settings.DownloadFolder, settings.FilenameTemplate);
            var outputFile = await _downloader.DownloadAsync(task.Url, task.FormatId, outputTemplate,
                (pct, speed, eta) =>
                {
                    task.Progress = pct;
                    task.Speed = speed;
                    db.Update(task);
                    QueueChanged?.Invoke();
                }, cts.Token);

            task.Status = "completed";
            task.Progress = 100;
            task.OutputFile = outputFile;
            task.FinishedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            db.Update(task);

            _history.Add(new HistoryEntry
            {
                Title = task.Title, Thumbnail = task.Thumbnail, Uploader = task.Uploader,
                FormatId = task.FormatId, QualityLabel = task.QualityLabel,
                SizeBytes = task.TotalBytes, OutputPath = outputFile,
                Status = "completed", SessionId = task.SessionId
            });
            QueueChanged?.Invoke();
        }
        catch (OperationCanceledException)
        {
            var db = _db.GetConnection();
            var task = db.Find<DownloadTask>(taskId);
            if (task != null) { task.Status = "cancelled"; db.Update(task); }
        }
        catch (Exception ex)
        {
            var db = _db.GetConnection();
            var task = db.Find<DownloadTask>(taskId);
            if (task != null)
            {
                task.RetryCount++;
                task.ErrorMessage = ex.Message;
                var settings = _settings.GetSettings();
                task.Status = task.RetryCount <= settings.MaxRetries ? "queued" : "failed";
                db.Update(task);
            }
        }
        finally
        {
            lock (_lock) _activeCount = Math.Max(0, _activeCount - 1);
            _cancellations.Remove(taskId);
            QueueChanged?.Invoke();
        }
    }
}
