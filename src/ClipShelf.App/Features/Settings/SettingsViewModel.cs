namespace ClipShelf.App.Features.Settings;

// Settings that apply immediately: each change is saved and takes effect without a restart.
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        DoubleClickOpensHistory = settings.Current.OpenHistoryOnDoubleClick;
    }

    [ObservableProperty]
    public partial bool DoubleClickOpensHistory { get; set; }

    partial void OnDoubleClickOpensHistoryChanged(bool value) =>
        _settings.Update(_settings.Current with { OpenHistoryOnDoubleClick = value });
}
