using System.Collections.ObjectModel;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Export;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;
using CommunityToolkit.Mvvm.Messaging;

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
    private readonly IClipExportService _export;
    private readonly ICategoryRepository _categories;
    private readonly ICategoryLockService _locks;
    private readonly ILockPasswordService _passwords;
    private readonly IUpdateChecker _updates;
    private readonly ILogger<SettingsViewModel> _log;
    private bool _checkedForUpdates;
    private CancellationTokenSource? _pruneCts;
    private bool _loading = true;

    public SettingsViewModel(
        SettingsService settings,
        IThemeService theme,
        IClipRepository repository,
        IStartupRegistration startup,
        IClipExportService export,
        ICategoryRepository categories,
        ICategoryLockService locks,
        ILockPasswordService passwords,
        IUpdateChecker updates,
        ILogger<SettingsViewModel> log)
    {
        _settings = settings;
        _theme = theme;
        _repository = repository;
        _startup = startup;
        _export = export;
        _categories = categories;
        _locks = locks;
        _passwords = passwords;
        _updates = updates;
        _log = log;

        SelectedTheme = ThemeOptions.FirstOrDefault(o => o.Value == settings.Current.Theme)
            ?? ThemeOptions.First(o => o.Value == AppTheme.Light);
        SelectedBackdrop = BackdropOptions.First(o => o.Value == settings.Current.Backdrop);
        SelectedTrayIcon = TrayIconOptions.First(o => o.Value == settings.Current.TrayIcon);
        SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Value == settings.Current.Language) ?? LanguageOptions[0];
        SelectedRetention = RetentionOptions.FirstOrDefault(o => o.Value == settings.Current.RetentionDays)
            ?? RetentionOptions[^1];
        ShowTrayIcon = settings.Current.ShowTrayIcon;
        DoubleClickOpensHistory = settings.Current.OpenHistoryOnDoubleClick;
        Hotkey = settings.Current.Hotkey;
        RunAtStartup = settings.Current.RunAtStartup;
        MaxItems = settings.Current.MaxItems;
        ClearOnSignOut = settings.Current.ClearOnSignOut;
        LockOnExit = settings.Current.LockOnExit;
        LockOnMinimize = settings.Current.LockOnMinimize;
        LockOnShutdown = settings.Current.LockOnShutdown;
        SelectedExportFormat = ExportFormatOptions[0];
        WeakReferenceMessenger.Default.Register<SettingsViewModel, DataChanged>(this, (vm, message) =>
        {
            if (message.Area == "category") _ = vm.RefreshCategoriesAsync();
        });

        _loading = false;
    }

    public IReadOnlyList<Option<AppTheme>> ThemeOptions { get; } =
    [
        new(AppTheme.System, Tr.Get("Theme_System")),
        new(AppTheme.Light, Tr.Get("Theme_Light")),
        new(AppTheme.Dark, Tr.Get("Theme_Dark")),
        new(AppTheme.Black, Tr.Get("Theme_Black")),
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

    // Language names are endonyms: they stay the same in every language.
    public IReadOnlyList<Option<string>> LanguageOptions { get; } =
    [
        new("en-US", "English"),
        new("ar-SA", "العربية"),
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
    public partial Option<string> SelectedLanguage { get; set; }

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

    [ObservableProperty]
    public partial bool LockOnExit { get; set; }

    [ObservableProperty]
    public partial bool LockOnMinimize { get; set; }

    [ObservableProperty]
    public partial bool LockOnShutdown { get; set; }

    // Summary on the "Lock categories when" dropdown: "on exit, on shutdown" or "never".
    public string LockWhenText
    {
        get
        {
            var parts = new List<string>(3);
            if (LockOnExit) parts.Add(Tr.Get("LockWhen_Exit"));
            if (LockOnMinimize) parts.Add(Tr.Get("LockWhen_Minimize"));
            if (LockOnShutdown) parts.Add(Tr.Get("LockWhen_Shutdown"));
            return parts.Count == 0 ? Tr.Get("LockWhen_Never") : string.Join(", ", parts);
        }
    }

    public IReadOnlyList<Option<ClipExportFormat>> ExportFormatOptions { get; } =
    [
        new(ClipExportFormat.Json, Tr.Get("Export_Json")),
        new(ClipExportFormat.Csv, Tr.Get("Export_Csv")),
    ];

    [ObservableProperty]
    public partial Option<ClipExportFormat> SelectedExportFormat { get; set; }

    [ObservableProperty]
    public partial bool IsExporting { get; set; }

    // Feedback under the export row: how many clips were written, or why it failed.
    [ObservableProperty]
    public partial string? ExportStatus { get; set; }

    // True while ExportStatus holds a failure, so the page shows it in red.
    [ObservableProperty]
    public partial bool IsExportError { get; set; }

    public bool CanExport => !IsExporting;

    public bool HasExportStatus => !string.IsNullOrEmpty(ExportStatus);

    // The shared password: asked before exporting and before opening a locked category.
    public bool HasLockPassword => _passwords.IsSet;

    public string PasswordButtonText =>
        HasLockPassword ? Tr.Get("Settings_ChangePassword") : Tr.Get("Settings_SetPassword");

    public bool VerifyLockPassword(string password) => _passwords.Verify(password);

    public async Task SetLockPasswordAsync(string password)
    {
        _passwords.Set(password);
        NotifyLockPasswordChanged();
        await RefreshCategoriesAsync();
    }

    public async Task RemoveLockPasswordAsync()
    {
        _passwords.Remove();
        _locks.Reset();
        await _categories.ClearLocksAsync(CancellationToken.None);
        NotifyLockPasswordChanged();
        await RefreshCategoriesAsync();
    }

    private void NotifyLockPasswordChanged()
    {
        OnPropertyChanged(nameof(HasLockPassword));
        OnPropertyChanged(nameof(PasswordButtonText));
    }

    // Update check: once per run from the page, or on demand from the button.
    [ObservableProperty]
    public partial string? UpdateStatus { get; set; }

    // True while UpdateStatus holds a failure, so the page shows it in red.
    [ObservableProperty]
    public partial bool IsUpdateError { get; set; }

    [ObservableProperty]
    public partial bool UpdateAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsCheckingUpdates { get; set; }

    public bool CanCheckUpdates => !IsCheckingUpdates;

    public bool HasUpdateStatus => !string.IsNullOrEmpty(UpdateStatus);

    public async Task CheckForUpdatesOnceAsync()
    {
        if (_checkedForUpdates) return;

        // A failed check is retried on the next visit; a successful one is not repeated.
        _checkedForUpdates = await CheckForUpdatesAsync();
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        if (IsCheckingUpdates) return false;

        IsCheckingUpdates = true;
        try
        {
            var newer = await _updates.NewerVersionAsync(CancellationToken.None);
            UpdateAvailable = newer is not null;
            UpdateStatus = newer is null
                ? Tr.Format("Settings_UpdateCurrent", UpdateChecker.CurrentVersion)
                : Tr.Format("Settings_UpdateAvailable", newer);
            IsUpdateError = false;
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Checking for updates failed");
            UpdateAvailable = false;
            UpdateStatus = Tr.Get("Settings_UpdateFailed");
            IsUpdateError = true;
            return false;
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    partial void OnIsCheckingUpdatesChanged(bool value) => OnPropertyChanged(nameof(CanCheckUpdates));

    partial void OnUpdateStatusChanged(string? value) => OnPropertyChanged(nameof(HasUpdateStatus));

    // Category locks: each category can hide its clips behind its own password.
    public ObservableCollection<CategoryRow> CategoryRows { get; } = [];

    public bool ShowCategoriesEmpty => CategoryRows.Count == 0;

    public async Task RefreshCategoriesAsync()
    {
        var categories = await _categories.GetCategoriesAsync(CancellationToken.None);

        CategoryRows.Clear();
        foreach (var category in categories)
        {
            CategoryRows.Add(new CategoryRow(
                category.Id,
                category.Name,
                category.IsLocked,
                _locks.UnlockedCategoryIds.Contains(category.Id)));
        }

        OnPropertyChanged(nameof(ShowCategoriesEmpty));
    }

    public async Task LockCategoryAsync(long id)
    {
        await _categories.SetLockedAsync(id, true, CancellationToken.None);
        _locks.Lock(id);
        await RefreshCategoriesAsync();
    }

    public async Task UnlockCategoryAsync(long id)
    {
        _locks.Unlock(id);
        await RefreshCategoriesAsync();
    }

    public async Task<bool> RenameCategoryAsync(long id, string name)
    {
        var renamed = await _categories.RenameAsync(id, name.Trim(), CancellationToken.None);
        if (renamed) await RefreshCategoriesAsync();
        return renamed;
    }

    public async Task DeleteCategoryAsync(long id)
    {
        await _categories.DeleteAsync(id, CancellationToken.None);
        _locks.Lock(id);
        await RefreshCategoriesAsync();
    }

    // Default name for the save dialog: ClipShelf-Export-2026-10-01-1432.
    public string SuggestedExportFileName =>
        Tr.Format("Settings_ExportFileName",
            Tr.Get("App_Name"),
            DateTimeOffset.Now.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture));

    // Settings search from the title bar; an empty filter shows everything.
    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    // Resx keys per settings group, in the page's order, so a search can hide whole groups.
    private static readonly string[][] Groups =
    [
        ["Settings_Theme", "Settings_Backdrop", "Theme_Black", "Settings_Language", "Settings_LanguageHint"],
        ["Settings_TrayIcon", "Settings_ShowTrayIcon", "Settings_DoubleClickTray"],
        [
            "Settings_RunAtStartup", "Settings_RunAtStartupHint",
            "Settings_Hotkey", "Settings_HotkeyHint",
        ],
        [
            "Settings_History", "Settings_MaxItems", "Settings_MaxItemsHint",
            "Settings_Retention", "Settings_RetentionHint",
            "Retention_Day", "Retention_Week", "Retention_Month", "Retention_Forever",
            "Settings_ClearOnSignOut", "Settings_ClearOnSignOutHint",
        ],
        ["Common_Export", "Settings_Export", "Settings_ExportHint"],
        [
            "Settings_Categories", "Settings_CategoriesEmpty",
            "Settings_LockPassword", "Settings_LockPasswordHint",
            "Settings_LockWhen", "Settings_LockOnExit", "Settings_LockOnMinimize", "Settings_LockOnShutdown",
            "Settings_LockCategory", "Settings_LockNow", "Settings_UnlockCategory", "Settings_RenameCategory",
        ],
        [
            "Settings_Updates", "Settings_CheckUpdates", "Settings_CheckUpdatesHint",
            "Settings_CheckUpdatesButton", "Settings_Download",
        ],
    ];

    private static readonly string[] ExitKeys = ["Settings_ExitApp", "Settings_ExitHint"];

    public bool ShowAppearanceGroup => IsGroupVisible(Filter, 0);

    public bool ShowTrayGroup => IsGroupVisible(Filter, 1);

    public bool ShowBehaviorGroup => IsGroupVisible(Filter, 2);

    public bool ShowHistoryGroup => IsGroupVisible(Filter, 3);

    public bool ShowExportGroup => IsGroupVisible(Filter, 4);

    public bool ShowCategoriesGroup => IsGroupVisible(Filter, 5);

    public bool ShowUpdatesGroup => IsGroupVisible(Filter, 6);

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
        OnPropertyChanged(nameof(ShowExportGroup));
        OnPropertyChanged(nameof(ShowCategoriesGroup));
        OnPropertyChanged(nameof(ShowUpdatesGroup));
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

    // AppSettings.Language takes effect on the next start; Culture.Configure reads it once.
    partial void OnSelectedLanguageChanged(Option<string> value) =>
        Save(_settings.Current with { Language = value.Value });

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

    partial void OnLockOnExitChanged(bool value)
    {
        Save(_settings.Current with { LockOnExit = value });
        OnPropertyChanged(nameof(LockWhenText));
    }

    partial void OnLockOnMinimizeChanged(bool value)
    {
        Save(_settings.Current with { LockOnMinimize = value });
        OnPropertyChanged(nameof(LockWhenText));
    }

    partial void OnLockOnShutdownChanged(bool value)
    {
        Save(_settings.Current with { LockOnShutdown = value });
        OnPropertyChanged(nameof(LockWhenText));
    }

    partial void OnIsExportingChanged(bool value) => OnPropertyChanged(nameof(CanExport));

    partial void OnExportStatusChanged(string? value) => OnPropertyChanged(nameof(HasExportStatus));

    // Runs after the page's save picker returned a file path.
    public async Task ExportAsync(string path)
    {
        if (IsExporting) return;

        IsExporting = true;
        ExportStatus = null;
        IsExportError = false;
        try
        {
            var count = await _export.ExportAsync(path, SelectedExportFormat.Value, CancellationToken.None);
            ExportStatus = count == 0
                ? Tr.Get("Settings_ExportEmpty")
                : Tr.Format("Settings_ExportDone", Tr.Plural("History_Count", count));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Exporting clips failed");
            ExportStatus = Tr.Get("Settings_ExportFailed");
            IsExportError = true;
        }
        finally
        {
            IsExporting = false;
        }
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
