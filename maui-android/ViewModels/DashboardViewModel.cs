using System.Windows.Input;
using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly QueueManagerService _queue;
    private readonly HistoryService _history;

    private int _totalDownloads;
    public int TotalDownloads { get => _totalDownloads; set => SetProperty(ref _totalDownloads, value); }

    private int _completedDownloads;
    public int CompletedDownloads { get => _completedDownloads; set => SetProperty(ref _completedDownloads, value); }

    private int _activeDownloads;
    public int ActiveDownloads { get => _activeDownloads; set => SetProperty(ref _activeDownloads, value); }

    private int _queuedDownloads;
    public int QueuedDownloads { get => _queuedDownloads; set => SetProperty(ref _queuedDownloads, value); }

    private int _failedDownloads;
    public int FailedDownloads { get => _failedDownloads; set => SetProperty(ref _failedDownloads, value); }

    private double _totalGbDownloaded;
    public double TotalGbDownloaded { get => _totalGbDownloaded; set => SetProperty(ref _totalGbDownloaded, value); }

    private double _successRate;
    public double SuccessRate { get => _successRate; set => SetProperty(ref _successRate, value); }

    public ICommand RefreshCommand { get; }

    public DashboardViewModel(QueueManagerService queue, HistoryService history)
    {
        _queue = queue;
        _history = history;
        RefreshCommand = new Command(Refresh);
        Refresh();
    }

    public void Refresh()
    {
        var tasks = _queue.GetTasks();
        TotalDownloads = _history.CountAll();
        CompletedDownloads = tasks.Count(t => t.Status == "completed");
        ActiveDownloads = tasks.Count(t => t.Status == "downloading");
        QueuedDownloads = tasks.Count(t => t.Status == "queued");
        FailedDownloads = tasks.Count(t => t.Status == "failed");
        TotalGbDownloaded = Math.Round((double)_history.SumSizeBytes() / (1024.0 * 1024 * 1024), 2);
        var attempts = CompletedDownloads + FailedDownloads;
        SuccessRate = attempts > 0 ? Math.Round((double)CompletedDownloads / attempts * 100, 1) : 0;
    }
}
