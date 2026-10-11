using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Views;
using System.Diagnostics;

namespace ClipShelf.Desktop.Shell;

// Owns the tray icon, the global hotkey and the app lifetime, mirroring the WinUI AppShellService
// minus the window specifics.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class TrayController : IHotkeyRegistration, IDisposable
{
    private readonly Lazy<MainWindow> _mainWindow;
    private readonly Lazy<FlyoutWindow> _flyout;
    private readonly GlobalHotkey _hotkey;
    private readonly SettingsService _settings;
    private readonly ISystemTheme _systemTheme;
    private readonly ICategoryLockService _locks;
    private readonly ILogger<TrayController> _log;
    private readonly TrayIcon _trayIcon = new();
    private bool _attached;
    private TrayIconKind? _appliedTrayIcon;
    private bool? _appliedShowTrayIcon;
    private string? _appliedTrayIconFile;
    private HotkeyGesture? _appliedHotkey;
    private readonly HotkeyRegistrationState _hotkeyState = new();

    public TrayController(
        Lazy<MainWindow> mainWindow,
        Lazy<FlyoutWindow> flyout,
        GlobalHotkey hotkey,
        SettingsService settings,
        ISystemTheme systemTheme,
        ICategoryLockService locks,
        ILogger<TrayController> log)
    {
        _mainWindow = mainWindow;
        _flyout = flyout;
        _hotkey = hotkey;
        _settings = settings;
        _systemTheme = systemTheme;
        _locks = locks;
        _log = log;

        _trayIcon.ToolTipText = Tr.Get("App_Name");
        _trayIcon.Clicked += OnTrayClicked;
        _trayIcon.Menu = BuildMenu();
        ApplyTraySettings();
        _settings.Changed += OnSettingsChanged;
        _systemTheme.Changed += OnSystemThemeChanged;
        _hotkey.Pressed += OnHotkeyPressed;
        ApplyHotkey();
    }

    // Attaches the icon to the application. Deferred until the lifetime's Startup event: the
    // platform tray backend is not ready during window initialization, and attaching early
    // silently never registers the icon.
    public void Attach()
    {
        if (_attached) return;
        _attached = true;

        // Only icons attached through Application.TrayIcons are shown; a standalone TrayIcon
        // never creates its platform icon. The item must be ADDED after the collection is set:
        // populating the list first raises no change event, so Attach never runs.
        if (Application.Current is { } app)
        {
            var icons = new TrayIcons();
            TrayIcon.SetIcons(app, icons);
            icons.Add(_trayIcon);
            _log.LogInformation("Tray attached through Application.TrayIcons");
        }
        else
        {
            _log.LogError("Application.Current was null; the tray icon was not attached");
        }
    }

    public bool IsCurrentHotkeyRegistered => _hotkeyState.Registered == _settings.Current.Hotkey;

    public HotkeyGesture? RegisteredHotkey => _hotkeyState.Registered;

    public void ShowHistory() => _mainWindow.Value.ShowHistory();

    public void ShowSettings() => _mainWindow.Value.ShowSettings();

    public void TogglePanel() => _flyout.Value.Toggle();

    public void Exit()
    {
        if (_settings.Current.LockOnExit) _locks.Reset();
        if (Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Ensure shutdown runs on UI thread
            Dispatcher.UIThread.Post(() => desktop.Shutdown());
        }
    }

    // A language or accent change needs a fresh process: start the new instance first, then let
    // this one go through the normal exit path (which releases the single-instance pipe).
    public void Restart()
    {
        try
        {
            if (Environment.ProcessPath is { } executable)
                Process.Start(executable);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Restarting the app failed");
        }

        Exit();
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        _systemTheme.Changed -= OnSystemThemeChanged;
        _hotkey.Pressed -= OnHotkeyPressed;
        if (Application.Current is { } app)
            TrayIcon.GetIcons(app)?.Remove(_trayIcon);
        _trayIcon.Dispose();
        _hotkey.Dispose();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => TogglePanel();

    private void OnHotkeyPressed(object? sender, EventArgs e) => TogglePanel();

    // Settings save as one record; re-apply only the parts that actually changed, so editing an
    // unrelated value never re-decodes the tray icon or re-registers the hotkey.
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        var current = _settings.Current;
        if (current.TrayIcon != _appliedTrayIcon || current.ShowTrayIcon != _appliedShowTrayIcon)
            ApplyTraySettings();
        if (current.Hotkey != _appliedHotkey)
            ApplyHotkey();
    }

    private void ApplyHotkey()
    {
        _appliedHotkey = _settings.Current.Hotkey;
        _hotkeyState.Apply(_appliedHotkey, gesture => _hotkey.Register(gesture));
    }

    // Picks the tray variant from the icon style and the taskbar theme, and honours show/hide.
    // The image goes first: making a imageless icon visible never registers it with the shell.
    private void ApplyTraySettings()
    {
        var current = _settings.Current;
        _appliedTrayIcon = current.TrayIcon;
        _appliedShowTrayIcon = current.ShowTrayIcon;
        ApplyTrayIcon(current.TrayIcon);
        _trayIcon.IsVisible = current.ShowTrayIcon;
        _log.LogInformation("Tray applied: visible {Visible}, file {File}", _trayIcon.IsVisible, _appliedTrayIconFile);
    }

    // Re-checks the resolved file first, so repeated theme notifications and settings saves that
    // resolve to the same variant never churn the shell icon.
    private void ApplyTrayIcon(TrayIconKind kind)
    {
        var file = TrayIconFiles.FileName(kind, _systemTheme.TaskbarIsLight);
        if (file == _appliedTrayIconFile) return;

        _appliedTrayIconFile = file;
        _trayIcon.Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", file));
    }

    // The registry watch fires on a pool thread, so every signal is queued to the UI thread the
    // tray icon lives on.
    private void OnSystemThemeChanged(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => ApplyTrayIcon(_settings.Current.TrayIcon));

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();
        menu.Items.Add(Item("Tray_OpenHistory", ShowHistory));
        menu.Items.Add(Item("Tray_Settings", ShowSettings));
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(Item("Tray_Exit", Exit));
        return menu;
    }

    private static NativeMenuItem Item(string textKey, Action action) =>
        new() { Header = Tr.Get(textKey), Command = new RelayCommand(action) };
}
