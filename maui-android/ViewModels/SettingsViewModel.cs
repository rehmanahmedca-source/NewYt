using System.Windows.Input;
using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settings;
    private SettingsRecord _record;

    public string DownloadFolder { get => _record.DownloadFolder; set { _record.DownloadFolder = value; OnPropertyChanged(); } }
    public string TempFolder { get => _record.TempFolder; set { _record.TempFolder = value; OnPropertyChanged(); } }
    public int MaxConcurrent { get => _record.MaxConcurrent; set { _record.MaxConcurrent = value; OnPropertyChanged(); } }
    public int ConcurrentFragments { get => _record.ConcurrentFragments; set { _record.ConcurrentFragments = value; OnPropertyChanged(); } }
    public int MaxRetries { get => _record.MaxRetries; set { _record.MaxRetries = value; OnPropertyChanged(); } }
    public int SpeedLimitKbps { get => _record.SpeedLimitKbps; set { _record.SpeedLimitKbps = value; OnPropertyChanged(); } }
    public string Proxy { get => _record.Proxy; set { _record.Proxy = value; OnPropertyChanged(); } }
    public string FfmpegPath { get => _record.FfmpegPath; set { _record.FfmpegPath = value; OnPropertyChanged(); } }
    public bool EmbedMetadata { get => _record.EmbedMetadata; set { _record.EmbedMetadata = value; OnPropertyChanged(); } }
    public bool EmbedThumbnail { get => _record.EmbedThumbnail; set { _record.EmbedThumbnail = value; OnPropertyChanged(); } }
    public bool EmbedSubtitles { get => _record.EmbedSubtitles; set { _record.EmbedSubtitles = value; OnPropertyChanged(); } }
    public bool Sponsorblock { get => _record.Sponsorblock; set { _record.Sponsorblock = value; OnPropertyChanged(); } }

    public ICommand SaveCommand { get; }

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        _record = _settings.GetSettings();
        SaveCommand = new Command(Save);
    }

    private void Save()
    {
        _settings.UpdateSettings(_record);
        Application.Current!.MainPage!.DisplayAlert("Success", "Settings saved", "OK");
    }
}
