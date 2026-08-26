using YTDownloaderXPro.ViewModels;

namespace YTDownloaderXPro.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;

    public HistoryPage(HistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        historyList.ItemsSource = _vm.Entries;
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => _vm.SearchText = e.NewTextValue ?? "";
    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        var id = Convert.ToInt32(((Button)sender!).CommandParameter);
        _vm.DeleteCommand.Execute(id);
    }
}
