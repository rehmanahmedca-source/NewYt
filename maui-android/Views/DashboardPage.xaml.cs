using YTDownloaderXPro.ViewModels;

namespace YTDownloaderXPro.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as DashboardViewModel)?.Refresh();
    }
}
