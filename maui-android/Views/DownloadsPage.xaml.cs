using YTDownloaderXPro.ViewModels;
using YTDownloaderXPro.Services;

namespace YTDownloaderXPro.Views;

public partial class DownloadsPage : ContentPage
{
    private readonly DownloadsViewModel _vm;

    public DownloadsPage(DownloadsViewModel vm, QueueManagerService queue, DownloaderService downloader)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
        entriesList.ItemsSource = _vm.Entries;
        queueList.ItemsSource = _vm.Tasks;
    }

    private async void OnFetchClicked(object? sender, EventArgs e)
    {
        _vm.FetchUrl = urlEntry.Text ?? "";
        await ((Command)_vm.FetchCommand).ExecuteAsync(null);
        resultsFrame.IsVisible = _vm.Entries.Count > 0;
        resultsTitle.Text = _vm.FetchTitle;
    }

    private void OnDownloadClicked(object? sender, EventArgs e) => _vm.DownloadSelectedCommand.Execute(null);
    private void OnPauseClicked(object? sender, EventArgs e) => _vm.PauseTask(((Button)sender!).CommandParameter?.ToString() ?? "");
    private void OnResumeClicked(object? sender, EventArgs e) => _vm.ResumeTask(((Button)sender!).CommandParameter?.ToString() ?? "");
    private void OnCancelClicked(object? sender, EventArgs e) => _vm.CancelTask(((Button)sender!).CommandParameter?.ToString() ?? "");
    private void OnRemoveClicked(object? sender, EventArgs e) => _vm.RemoveTask(((Button)sender!).CommandParameter?.ToString() ?? "");
}
