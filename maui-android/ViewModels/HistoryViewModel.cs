using System.Collections.ObjectModel;
using System.Windows.Input;
using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.ViewModels;

public class HistoryViewModel : BaseViewModel
{
    private readonly HistoryService _history;

    private string _searchText = "";
    public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); Refresh(); } }

    public ObservableCollection<HistoryEntry> Entries { get; } = new();
    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }

    public HistoryViewModel(HistoryService history)
    {
        _history = history;
        RefreshCommand = new Command(Refresh);
        DeleteCommand = new Command<int>(Delete);
        Refresh();
    }

    public void Refresh()
    {
        var entries = _history.List(SearchText);
        Entries.Clear();
        foreach (var e in entries) Entries.Add(e);
    }

    private void Delete(int id)
    {
        _history.Remove(id);
        Refresh();
    }
}
