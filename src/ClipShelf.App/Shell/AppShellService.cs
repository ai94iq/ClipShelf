using ClipShelf.App.Platform;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ClipShelf.App.Shell;

// Owns the tray icon, the global hotkey and the app's window lifetime. Because closing a window
// only hides it, the process stays alive in the notification area until the user picks Exit.
public sealed class AppShellService : IDisposable
{
    private readonly Lazy<MainWindow> _mainWindow;
    private readonly Lazy<Features.ClipboardPanel.ClipboardPanelWindow> _panel;
    private readonly GlobalHotkey _hotkey;
    private readonly SettingsService _settings;
    private readonly TaskbarIcon _trayIcon = new();
    private TrayIconKind? _appliedTrayIcon;
    private bool? _appliedShowTrayIcon;
    private HotkeyGesture? _appliedHotkey;

    public AppShellService(
        Lazy<MainWindow> mainWindow,
        Lazy<Features.ClipboardPanel.ClipboardPanelWindow> panel,
        GlobalHotkey hotkey,
        SettingsService settings,
        AppLifetime lifetime)
    {
        _mainWindow = mainWindow;
        _panel = panel;
        _hotkey = hotkey;
        _settings = settings;
        Lifetime = lifetime;

        _trayIcon.ToolTipText = Tr.Get("App_Name");
        // Don't delay the flyout while waiting to see whether a second click follows.
        _trayIcon.NoLeftClickDelay = true;
        _trayIcon.LeftClickCommand = new AsyncRelayCommand(TogglePanelAsync);
        _trayIcon.DoubleClickCommand = new RelayCommand(OpenHistoryOnDoubleClick);
        _trayIcon.ContextFlyout = BuildMenu();
        _trayIcon.ForceCreate();
        ApplyTraySettings();
        _settings.Changed += (_, _) => OnSettingsChanged();

        _hotkey.Pressed += (_, _) => _ = TogglePanelAsync();
        ApplyHotkey();

        // Create the flyout window now so the first open is instant instead of paying
        // the full XAML and backdrop setup while the user is waiting on the hotkey.
        _ = _panel.Value;
    }

    public AppLifetime Lifetime { get; }

    // Used by the settings page to anchor system pickers to the app window.
    public IntPtr MainWindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow.Value);

    public event EventHandler? ExitRequested;

    public void ShowHistory() => _mainWindow.Value.ShowHistory();

    public void ShowSettings() => _mainWindow.Value.ShowSettings();

    public void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _trayIcon.Dispose();
        _hotkey.Dispose();
    }

    private Task TogglePanelAsync() => _panel.Value.ToggleAsync();

    // Settings save as one record; re-apply only the parts that actually changed, so editing an
    // unrelated value never re-decodes the tray icon or re-registers the hotkey.
    private void OnSettingsChanged()
    {
        var current = _settings.Current;
        if (current.TrayIcon != _appliedTrayIcon || current.ShowTrayIcon != _appliedShowTrayIcon)
            ApplyTraySettings();
        if (current.Hotkey != _appliedHotkey)
            ApplyHotkey();
    }

    // Windows reserves Win+V for its own clipboard, so the default is Win+Shift+V; the user can
    // change it on the Settings page and this re-registers on every change.
    private void ApplyHotkey()
    {
        _appliedHotkey = _settings.Current.Hotkey;
        _hotkey.Register((uint)_appliedHotkey.Modifiers, _appliedHotkey.VirtualKey);
    }

    private void OpenHistoryOnDoubleClick()
    {
        if (!_settings.Current.OpenHistoryOnDoubleClick) return;

        _panel.Value.Hide();
        ShowHistory();
    }

    // Picks the tray variant from the icon style and the taskbar theme, and honours show/hide.
    private void ApplyTraySettings()
    {
        var current = _settings.Current;
        _appliedTrayIcon = current.TrayIcon;
        _appliedShowTrayIcon = current.ShowTrayIcon;
        _trayIcon.Visibility = current.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
        _trayIcon.IconSource = new BitmapImage(new Uri(Path.Combine(
            AppContext.BaseDirectory, "Assets", TrayIconFile(current.TrayIcon))));
    }

    private static string TrayIconFile(TrayIconKind kind)
    {
        var variant = SystemTheme.TaskbarIsLight()
            ? kind == TrayIconKind.Outline ? "tray-outline-light.ico" : "tray-light.ico"
            : kind == TrayIconKind.Outline ? "tray-outline-dark.ico" : "tray-dark.ico";

        return variant;
    }

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Tray_OpenHistory", ShowHistory));
        menu.Items.Add(Item("Tray_Settings", ShowSettings));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Tray_Exit", Exit));
        return menu;
    }

    private static MenuFlyoutItem Item(string textKey, Action action) =>
        new() { Text = Tr.Get(textKey), Command = new RelayCommand(action) };
}
