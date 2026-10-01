using ClipShelf.App.Platform;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ClipShelf.App.Shell;

// Owns the tray icon, the global hotkey and the app's window lifetime. Because closing a window
// only hides it, the process stays alive in the notification area until the user picks Exit.
public sealed class AppShellService : IDisposable
{
    // Windows reserves Win+V for its own clipboard, so the default is Win+Shift+V.
    private const uint HotkeyVirtualKey = 0x56; // V

    private readonly Lazy<MainWindow> _mainWindow;
    private readonly Lazy<Features.ClipboardPanel.ClipboardPanelWindow> _panel;
    private readonly GlobalHotkey _hotkey;
    private readonly SettingsService _settings;
    private readonly TaskbarIcon _trayIcon = new();

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
        _trayIcon.IconSource = new BitmapImage(new Uri(Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            SystemTheme.TaskbarIsLight() ? "tray-light.ico" : "tray-dark.ico")));
        _trayIcon.LeftClickCommand = new AsyncRelayCommand(TogglePanelAsync);
        _trayIcon.DoubleClickCommand = new RelayCommand(OpenHistoryOnDoubleClick);
        _trayIcon.ContextFlyout = BuildMenu();
        _trayIcon.ForceCreate();

        _hotkey.Pressed += (_, _) => _ = TogglePanelAsync();
        _hotkey.Register(NativeMethods.ModWin | NativeMethods.ModShift, HotkeyVirtualKey);
    }

    public AppLifetime Lifetime { get; }

    public event EventHandler? ExitRequested;

    public void ShowHistory() => _mainWindow.Value.ShowHistory();

    public void ShowSettings() => _mainWindow.Value.ShowSettings();

    public void Dispose()
    {
        _trayIcon.Dispose();
        _hotkey.Dispose();
    }

    private Task TogglePanelAsync() => _panel.Value.ToggleAsync();

    private void OpenHistoryOnDoubleClick()
    {
        if (_settings.Current.OpenHistoryOnDoubleClick) ShowHistory();
    }

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Tray_OpenHistory", ShowHistory));
        menu.Items.Add(Item("Tray_Settings", ShowSettings));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Tray_Exit", RequestExit));
        return menu;
    }

    private static MenuFlyoutItem Item(string textKey, Action action) =>
        new() { Text = Tr.Get(textKey), Command = new RelayCommand(action) };

    private void RequestExit() => ExitRequested?.Invoke(this, EventArgs.Empty);
}
