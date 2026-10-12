using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform;

namespace ClipShelf.Desktop.Shell;

// Owns the tray icon, the global hotkey and the app's window lifetime. Closing a window only
// hides it, so the process stays in the notification area until the user picks Exit.
public sealed class AppShellService : IDisposable, IHotkeyRegistration
{
    private readonly IGlobalHotkey _hotkey;
    private readonly SettingsService _settings;
    private readonly ISystemTheme _systemTheme;
    private readonly ICategoryLockService _locks;
    private readonly Lazy<MainWindow> _mainWindow;
    private readonly ILogger<AppShellService> _log;
    private readonly HotkeyRegistrationState _hotkeyState = new();
    private TrayIcon? _tray;
    private string? _appliedTrayIconFile;
    private HotkeyGesture? _appliedHotkey;

    public AppShellService(
        IGlobalHotkey hotkey,
        SettingsService settings,
        ISystemTheme systemTheme,
        ICategoryLockService locks,
        AppLifetime lifetime,
        Lazy<MainWindow> mainWindow,
        ILogger<AppShellService> log)
    {
        _hotkey = hotkey;
        _settings = settings;
        _systemTheme = systemTheme;
        _locks = locks;
        _mainWindow = mainWindow;
        _log = log;
        Lifetime = lifetime;

        _settings.Changed += (_, _) => OnSettingsChanged();
        _hotkey.Pressed += (_, _) => TogglePanel();
        ApplyHotkey();
    }

    public AppLifetime Lifetime { get; }

    public bool IsCurrentHotkeyRegistered => _hotkeyState.Registered == _settings.Current.Hotkey;

    public HotkeyGesture? RegisteredHotkey => _hotkeyState.Registered;

    public event EventHandler? ExitRequested;

    public void ShowHistory() => _mainWindow.Value.ShowHistory();

    public void ShowSettings() => _mainWindow.Value.ShowSettings();

    // ponytail: the flyout arrives in batch 4; until then this only logs.
    public void TogglePanel() => _log.LogInformation("Flyout toggle requested");

    public void Exit()
    {
        if (_settings.Current.LockOnExit) _locks.Reset();
        Lifetime.IsExiting = true;           // windows close for real from here on
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    // Created on the UI thread once the app is running. Avalonia 12.1.3+ only gives a tray icon
    // its native implementation once it is attached to the application's tray collection.
    public void ShowTray()
    {
        _tray = new TrayIcon
        {
            ToolTipText = Tr.Get("App_Name"),
            Menu = BuildMenu(),
        };
        // ponytail: Avalonia's tray has no double-click event, so "double-click opens History"
        // is not wired; add it with a click-timing check if the setting must keep working.
        _tray.Clicked += (_, _) => TogglePanel();
        TrayIcon.SetIcons(Application.Current!, [_tray]);
        ApplyTraySettings();
        _systemTheme.Changed += OnSystemThemeChanged;
    }

    public void Dispose()
    {
        _systemTheme.Changed -= OnSystemThemeChanged;
        _tray?.Dispose();
        _hotkey.Dispose();
    }

    // Settings save as one record; re-apply only the parts that changed.
    private void OnSettingsChanged()
    {
        ApplyTraySettings();
        if (_settings.Current.Hotkey != _appliedHotkey) ApplyHotkey();
    }

    // A taken shortcut keeps the previous one working; Settings reads the outcome.
    private void ApplyHotkey()
    {
        _appliedHotkey = _settings.Current.Hotkey;
        if (!_hotkeyState.Apply(_appliedHotkey, gesture => _hotkey.Register(gesture)))
            _log.LogWarning("Hotkey {Hotkey} could not be registered", HotkeyText.Format(_appliedHotkey));
    }

    private void ApplyTraySettings()
    {
        if (_tray is null) return;
        _tray.IsVisible = _settings.Current.ShowTrayIcon;
        ApplyTrayIcon();
    }

    // Re-checks the resolved file first, so repeated theme signals never churn the shell icon.
    private void ApplyTrayIcon()
    {
        var file = TrayIconFiles.FileName(_settings.Current.TrayIcon, _systemTheme.TaskbarIsLight);
        if (_tray is null || file == _appliedTrayIconFile) return;

        _appliedTrayIconFile = file;
        _tray.Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", file));
    }

    // The registry watch fires on a pool thread; the tray lives on the UI thread.
    private void OnSystemThemeChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(ApplyTrayIcon);

    private NativeMenu BuildMenu() =>
    [
        Item("Tray_OpenHistory", ShowHistory),
        Item("Tray_Settings", ShowSettings),
        new NativeMenuItemSeparator(),
        Item("Tray_Exit", Exit),
    ];

    private static NativeMenuItem Item(string textKey, Action action)
    {
        var item = new NativeMenuItem(Tr.Get(textKey));
        item.Click += (_, _) => action();
        return item;
    }
}
