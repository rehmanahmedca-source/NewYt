using YTDownloaderXPro.ViewModels;

namespace YTDownloaderXPro.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
