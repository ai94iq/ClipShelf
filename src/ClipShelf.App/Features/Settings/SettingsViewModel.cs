using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Features.Settings;

// Settings that apply immediately: each change is saved and takes effect without a restart.
public sealed partial class SettingsViewModel : ObservableObject
{
    private const int MinHistoryItems = 5;
    private const int MaxHistoryItems = 1000;
    private const int PruneDelayMs = 300;

    private readonly SettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IClipRepository _repository;
    private readonly IStartupRegistration _startup;
    private CancellationTokenSource? _pruneCts;
    private bool _loading = true;

    public SettingsViewModel(
        SettingsService settings,
        IThemeService theme,
        IClipRepository repository,
        IStartupRegistration startup)
    {
        _settings = settings;
        _theme = theme;
        _repository = repository;
        _startup = startup;

        SelectedTheme = ThemeOptions.First(o => o.Value == settings.Current.Theme);
        SelectedBackdrop = BackdropOptions.First(o => o.Value == settings.Current.Backdrop);
        SelectedTrayIcon = TrayIconOptions.First(o => o.Value == settings.Current.TrayIcon);
        SelectedRetention = RetentionOptions.FirstOrDefault(o => o.Value == settings.Current.RetentionDays)
            ?? RetentionOptions[^1];
        ShowTrayIcon = settings.Current.ShowTrayIcon;
        DoubleClickOpensHistory = settings.Current.OpenHistoryOnDoubleClick;
        Hotkey = settings.Current.Hotkey;
        RunAtStartup = settings.Current.RunAtStartup;
        MaxItems = settings.Current.MaxItems;
        ClearOnSignOut = settings.Current.ClearOnSignOut;

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

    public IReadOnlyList<Option<int>> RetentionOptions { get; } =
    [
        new(1, Tr.Get("Retention_Day")),
        new(7, Tr.Get("Retention_Week")),
        new(30, Tr.Get("Retention_Month")),
        new(0, Tr.Get("Retention_Forever")),
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

    [ObservableProperty]
    public partial HotkeyGesture Hotkey { get; set; }

    [ObservableProperty]
    public partial bool RunAtStartup { get; set; }

    [ObservableProperty]
    public partial double MaxItems { get; set; }

    [ObservableProperty]
    public partial Option<int> SelectedRetention { get; set; }

    [ObservableProperty]
    public partial bool ClearOnSignOut { get; set; }

    // Settings search from the title bar; an empty filter shows everything.
    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    // Resx keys per settings group, in the page's order, so a search can hide whole groups.
    private static readonly string[][] Groups =
    [
        ["Settings_Theme", "Settings_Backdrop"],
        ["Settings_TrayIcon", "Settings_ShowTrayIcon", "Settings_DoubleClickTray"],
        [
            "Settings_RunAtStartup", "Settings_RunAtStartupHint",
            "Settings_Hotkey", "Settings_HotkeyHint",
            "Settings_MaxItems", "Settings_MaxItemsHint",
        ],
        [
            "Settings_History", "Settings_Retention", "Settings_RetentionHint",
            "Retention_Day", "Retention_Week", "Retention_Month", "Retention_Forever",
            "Settings_ClearOnSignOut", "Settings_ClearOnSignOutHint",
        ],
    ];

    private static readonly string[] ExitKeys = ["Settings_ExitApp", "Settings_ExitHint"];

    public bool ShowAppearanceGroup => IsGroupVisible(Filter, 0);

    public bool ShowTrayGroup => IsGroupVisible(Filter, 1);

    public bool ShowBehaviorGroup => IsGroupVisible(Filter, 2);

    public bool ShowHistoryGroup => IsGroupVisible(Filter, 3);

    public bool ShowExitGroup => IsExitVisible(Filter);

    public bool ShowNoMatches => HasNoMatches(Filter);

    public bool IsGroupVisible(string filter, int index) => MatchesAny(filter, Groups[index]);

    public bool IsExitVisible(string filter) => MatchesAny(filter, ExitKeys);

    public bool HasNoMatches(string filter) =>
        !Groups.Any(group => MatchesAny(filter, group)) && !MatchesAny(filter, ExitKeys);

    partial void OnFilterChanged(string value)
    {
        OnPropertyChanged(nameof(ShowAppearanceGroup));
        OnPropertyChanged(nameof(ShowTrayGroup));
        OnPropertyChanged(nameof(ShowBehaviorGroup));
        OnPropertyChanged(nameof(ShowHistoryGroup));
        OnPropertyChanged(nameof(ShowExitGroup));
        OnPropertyChanged(nameof(ShowNoMatches));
    }

    private static bool MatchesAny(string filter, string[] keys) =>
        string.IsNullOrWhiteSpace(filter) ||
        keys.Any(key => Tr.Get(key).Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase));

    public string HotkeyDisplay => HotkeyText.Format(Hotkey);

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

    partial void OnHotkeyChanged(HotkeyGesture value)
    {
        OnPropertyChanged(nameof(HotkeyDisplay));
        if (_loading) return;

        Save(_settings.Current with { Hotkey = value });
    }

    partial void OnRunAtStartupChanged(bool value)
    {
        if (_loading) return;

        Save(_settings.Current with { RunAtStartup = value });
        _startup.Apply(value);
    }

    partial void OnMaxItemsChanged(double value)
    {
        if (_loading) return;

        var keep = (int)Math.Clamp(value, MinHistoryItems, MaxHistoryItems);
        Save(_settings.Current with { MaxItems = keep });
        PruneDebounced(keep);
    }

    // The number box reports every keystroke or spin click; trim once the value settles instead
    // of deleting rows on each intermediate value.
    private void PruneDebounced(int keep)
    {
        _pruneCts?.Cancel();
        var cts = _pruneCts = new CancellationTokenSource();
        _ = PruneAfterDelayAsync(keep, cts.Token);
    }

    private async Task PruneAfterDelayAsync(int keep, CancellationToken ct)
    {
        try
        {
            await Task.Delay(PruneDelayMs, ct);
            await _repository.PruneAsync(keep, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer value
        }
    }

    partial void OnSelectedRetentionChanged(Option<int> value)
    {
        if (_loading) return;

        Save(_settings.Current with { RetentionDays = value.Value });
        if (value.Value > 0)
            _ = _repository.PruneOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-value.Value), CancellationToken.None);
    }

    partial void OnClearOnSignOutChanged(bool value)
    {
        if (_loading) return;

        Save(_settings.Current with { ClearOnSignOut = value });
    }

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
