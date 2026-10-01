using ClipShelf.App.Features.ClipboardPanel;
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

    private readonly Lazy<MainWindow> _settingsWindow;
    private readonly Lazy<ClipboardPanelWindow> _panel;
    private readonly GlobalHotkey _hotkey;
    private readonly TaskbarIcon _trayIcon = new();

    public AppShellService(
        Lazy<MainWindow> settingsWindow,
        Lazy<ClipboardPanelWindow> panel,
        GlobalHotkey hotkey,
        AppLifetime lifetime)
    {
        _settingsWindow = settingsWindow;
        _panel = panel;
        _hotkey = hotkey;
        Lifetime = lifetime;

        _trayIcon.ToolTipText = Tr.Get("App_Name");
        _trayIcon.IconSource = new BitmapImage(new Uri(Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            SystemTheme.TaskbarIsLight() ? "tray-light.ico" : "tray-dark.ico")));
        _trayIcon.NoLeftClickDelay = true;
        _trayIcon.LeftClickCommand = new AsyncRelayCommand(TogglePanelAsync);
        _trayIcon.ContextFlyout = BuildMenu();
        _trayIcon.ForceCreate();

        _hotkey.Pressed += (_, _) => _ = TogglePanelAsync();
        _hotkey.Register(NativeMethods.ModWin | NativeMethods.ModShift, HotkeyVirtualKey);
    }

    public AppLifetime Lifetime { get; }

    public event EventHandler? ExitRequested;

    public void ShowSettings()
    {
        var window = _settingsWindow.Value;
        window.AppWindow.Show();
        window.Activate();
    }

    public void Dispose()
    {
        _trayIcon.Dispose();
        _hotkey.Dispose();
    }

    private Task TogglePanelAsync() => _panel.Value.ToggleAsync();

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Tray_OpenHistory", () => _ = TogglePanelAsync()));
        menu.Items.Add(Item("Tray_Settings", ShowSettings));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Tray_Exit", RequestExit));
        return menu;
    }

    private static MenuFlyoutItem Item(string textKey, Action action) =>
        new() { Text = Tr.Get(textKey), Command = new RelayCommand(action) };

    private void RequestExit() => ExitRequested?.Invoke(this, EventArgs.Empty);
}
