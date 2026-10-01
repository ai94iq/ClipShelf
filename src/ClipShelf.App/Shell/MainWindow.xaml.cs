using ClipShelf.App.Features.History;
using ClipShelf.App.Features.Settings;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Shell;

// The app shell: a NavigationView with the History and Settings pages. Closing it only hides it.
public sealed partial class MainWindow : Window
{
    private readonly Lazy<HistoryPage> _history;
    private readonly Lazy<SettingsPage> _settings;
    private readonly AppLifetime _lifetime;
    private readonly WindowContext _context;

    public MainWindow(
        Lazy<HistoryPage> history,
        Lazy<SettingsPage> settings,
        IThemeService theme,
        AppLifetime lifetime,
        WindowContext context)
    {
        _history = history;
        _settings = settings;
        _lifetime = lifetime;
        _context = context;

        InitializeComponent();
        Title = Tr.Get("App_Name");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.ApplyCultureDirection();
        theme.Attach(this);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(940, 660));

        // Closing only hides the window; the app keeps running in the tray.
        AppWindow.Closing += (_, args) =>
        {
            if (_lifetime.IsExiting) return;
            args.Cancel = true;
            AppWindow.Hide();
        };

        Activated += (_, args) =>
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated) _context.Active = this;
        };

        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop();
        else
            Root.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];

        Nav.SelectedItem = Nav.MenuItems[0];
    }

    public void ShowHistory()
    {
        Nav.SelectedItem = Nav.MenuItems[0];
        AppWindow.Show();
        Activate();
        _ = _history.Value.LoadAsync();
    }

    public void ShowSettings()
    {
        Nav.SelectedItem = Nav.MenuItems[1];
        AppWindow.Show();
        Activate();
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        if ((item.Tag as string) == "settings")
        {
            ContentFrame.Content = _settings.Value;
            return;
        }

        var page = _history.Value;
        ContentFrame.Content = page;
        _ = page.LoadAsync();
    }
}
