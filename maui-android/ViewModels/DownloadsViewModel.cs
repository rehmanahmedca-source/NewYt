using System.Collections.ObjectModel;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.ViewModels;

public class DownloadsViewModel : BaseViewModel
{
    private readonly QueueManagerService _queue;
    private readonly DownloaderService _downloader;

    private string _fetchUrl = "";
    public string FetchUrl { get => _fetchUrl; set => SetProperty(ref _fetchUrl, value); }

    private bool _isFetching;
    public bool IsFetching { get => _isFetching; set => SetProperty(ref _isFetching, value); }

    private string _fetchTitle = "";
    public string FetchTitle { get => _fetchTitle; set => SetProperty(ref _fetchTitle, value); }

    public ObservableCollection<VideoEntry> Entries { get; } = new();
    public ObservableCollection<DownloadTask> Tasks { get; } = new();

    public ICommand FetchCommand { get; }
    public ICommand DownloadSelectedCommand { get; }
    public ICommand RefreshQueueCommand { get; }

    public DownloadsViewModel(QueueManagerService queue, DownloaderService downloader)
    {
        _queue = queue;
        _downloader = downloader;
        FetchCommand = new Command(async () => await FetchAsync());
        DownloadSelectedCommand = new Command(DownloadSelected);
        RefreshQueueCommand = new Command(RefreshQueue);

        _queue.QueueChanged += () => MainThread.BeginInvokeOnMainThread(RefreshQueue);
        RefreshQueue();
    }

    private async Task FetchAsync()
    {
        if (string.IsNullOrWhiteSpace(FetchUrl)) return;
        IsFetching = true;
        try
        {
            var info = await _downloader.FetchOverviewAsync(FetchUrl);
            Entries.Clear();
            FetchTitle = info["title"]?.ToString() ?? "Results";

            var entries = info["entries"] as JArray;
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    Entries.Add(new VideoEntry
                    {
                        Id = e["id"]?.ToString() ?? "",
                        Url = e["url"]?.ToString() ?? FetchUrl,
                        Title = e["title"]?.ToString() ?? "Untitled",
                        Thumbnail = e["thumbnail"]?.ToString() ?? "",
                        Uploader = e["uploader"]?.ToString() ?? ""
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsFetching = false; }
    }

    private void DownloadSelected()
    {
        foreach (var entry in Entries.Where(e => e.IsSelected))
        {
            _queue.AddTask(entry.Url, "bestvideo+bestaudio/best", "Best",
                entry.Title, entry.Thumbnail, entry.Uploader);
        }
    }

    public void RefreshQueue()
    {
        var tasks = _queue.GetTasks();
        Tasks.Clear();
        foreach (var t in tasks) Tasks.Add(t);
    }

    public void PauseTask(string id) => _queue.Pause(id);
    public void ResumeTask(string id) => _queue.Resume(id);
    public void CancelTask(string id) => _queue.Cancel(id);
    public void RemoveTask(string id) => _queue.Remove(id);
}

public class VideoEntry
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public string Uploader { get; set; } = "";
    public bool IsSelected { get; set; } = true;
}
