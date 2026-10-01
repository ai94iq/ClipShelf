using ClipShelf.App.Services;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Features.Settings;

// Settings that apply immediately: each change is saved and takes effect without a restart.
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly IThemeService _theme;
    private bool _loading = true;

    public SettingsViewModel(SettingsService settings, IThemeService theme)
    {
        _settings = settings;
        _theme = theme;

        SelectedTheme = ThemeOptions.First(o => o.Value == settings.Current.Theme);
        SelectedBackdrop = BackdropOptions.First(o => o.Value == settings.Current.Backdrop);
        SelectedTrayIcon = TrayIconOptions.First(o => o.Value == settings.Current.TrayIcon);
        ShowTrayIcon = settings.Current.ShowTrayIcon;
        DoubleClickOpensHistory = settings.Current.OpenHistoryOnDoubleClick;

        _loading = false;
    }

    public IReadOnlyList<Option<AppTheme>> ThemeOptions { get; } =
    [
        new(AppTheme.System, Tr.Get("Theme_System")),
        new(AppTheme.Light, Tr.Get("Theme_Light")),
        new(AppTheme.Dark, Tr.Get("Theme_Dark")),
    ];

    public IReadOnlyList<Option<BackdropKind>> BackdropOptions { get; } =
    [
        new(BackdropKind.Mica, Tr.Get("Backdrop_Mica")),
        new(BackdropKind.MicaAlt, Tr.Get("Backdrop_MicaAlt")),
        new(BackdropKind.Acrylic, Tr.Get("Backdrop_Acrylic")),
        new(BackdropKind.None, Tr.Get("Backdrop_None")),
    ];

    public IReadOnlyList<Option<TrayIconKind>> TrayIconOptions { get; } =
    [
        new(TrayIconKind.Filled, Tr.Get("TrayIcon_Filled")),
        new(TrayIconKind.Outline, Tr.Get("TrayIcon_Outline")),
    ];

    [ObservableProperty]
    public partial Option<AppTheme> SelectedTheme { get; set; }

    [ObservableProperty]
    public partial Option<BackdropKind> SelectedBackdrop { get; set; }

    [ObservableProperty]
    public partial Option<TrayIconKind> SelectedTrayIcon { get; set; }

    [ObservableProperty]
    public partial bool ShowTrayIcon { get; set; }

    [ObservableProperty]
    public partial bool DoubleClickOpensHistory { get; set; }

    partial void OnSelectedThemeChanged(Option<AppTheme> value) =>
        ApplyTheme(_settings.Current with { Theme = value.Value });

    partial void OnSelectedBackdropChanged(Option<BackdropKind> value) =>
        ApplyTheme(_settings.Current with { Backdrop = value.Value });

    partial void OnSelectedTrayIconChanged(Option<TrayIconKind> value) =>
        Save(_settings.Current with { TrayIcon = value.Value });

    partial void OnShowTrayIconChanged(bool value) =>
        Save(_settings.Current with { ShowTrayIcon = value });

    partial void OnDoubleClickOpensHistoryChanged(bool value) =>
        Save(_settings.Current with { OpenHistoryOnDoubleClick = value });

    // Theme and backdrop show on screen right away, so they are re-applied as well as saved.
    private void ApplyTheme(AppSettings next)
    {
        if (_loading) return;

        _settings.Update(next);
        _theme.Apply(next);
    }

    private void Save(AppSettings next)
    {
        if (_loading) return;

        _settings.Update(next);
    }
}
