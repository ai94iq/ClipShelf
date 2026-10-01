using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ClipShelf.App.Shell;

// Owns the tray icon and the menu behind it. Because closing a window only hides it, the process
// stays alive in the notification area until the user picks Exit.
public sealed class AppShellService : IDisposable
{
    private readonly Lazy<MainWindow> _settingsWindow;
    private readonly TaskbarIcon _trayIcon = new();

    public AppShellService(Lazy<MainWindow> settingsWindow, AppLifetime lifetime)
    {
        _settingsWindow = settingsWindow;
        Lifetime = lifetime;

        _trayIcon.ToolTipText = Tr.Get("App_Name");
        _trayIcon.IconSource = new BitmapImage(
            new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico")));
        _trayIcon.NoLeftClickDelay = true;
        _trayIcon.ContextFlyout = BuildMenu();
        _trayIcon.ForceCreate();
    }

    public AppLifetime Lifetime { get; }

    public event EventHandler? ExitRequested;

    public void ShowSettings()
    {
        var window = _settingsWindow.Value;
        window.AppWindow.Show();
        window.Activate();
    }

    public void Dispose() => _trayIcon.Dispose();

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Tray_Settings", ShowSettings));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Tray_Exit", RequestExit));
        return menu;
    }

    private static MenuFlyoutItem Item(string textKey, Action action) =>
        new() { Text = Tr.Get(textKey), Command = new RelayCommand(action) };

    private void RequestExit() => ExitRequested?.Invoke(this, EventArgs.Empty);
}
