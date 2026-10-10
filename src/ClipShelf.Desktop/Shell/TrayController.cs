using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Views;

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
    private readonly TrayIcon _trayIcon = new();
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
        ICategoryLockService locks)
    {
        _mainWindow = mainWindow;
        _flyout = flyout;
        _hotkey = hotkey;
        _settings = settings;
        _systemTheme = systemTheme;
        _locks = locks;

        _trayIcon.ToolTipText = Tr.Get("App_Name");
        _trayIcon.Clicked += OnTrayClicked;
        _trayIcon.Menu = BuildMenu();
        ApplyTraySettings();
        _settings.Changed += OnSettingsChanged;
        _systemTheme.Changed += OnSystemThemeChanged;
        _hotkey.Pressed += OnHotkeyPressed;
        ApplyHotkey();
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
            desktop.Shutdown();
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        _systemTheme.Changed -= OnSystemThemeChanged;
        _hotkey.Pressed -= OnHotkeyPressed;
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
    private void ApplyTraySettings()
    {
        var current = _settings.Current;
        _appliedTrayIcon = current.TrayIcon;
        _appliedShowTrayIcon = current.ShowTrayIcon;
        _trayIcon.IsVisible = current.ShowTrayIcon;
        ApplyTrayIcon(current.TrayIcon);
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
