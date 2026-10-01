using ClipShelf.App.Features.History;
using ClipShelf.App.Features.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;

namespace ClipShelf.App.Shell;

// The app shell: a NavigationView with the History and Settings pages. Closing it only hides it.
public sealed partial class MainWindow : Window
{
    private readonly Lazy<HistoryPage> _history;
    private readonly Lazy<SettingsPage> _settings;
    private readonly AppLifetime _lifetime;
    private readonly SettingsService _appSettings;
    private readonly ICategoryLockService _locks;

    public MainWindow(
        Lazy<HistoryPage> history,
        Lazy<SettingsPage> settings,
        IThemeService theme,
        AppLifetime lifetime,
        SettingsService appSettings,
        ICategoryLockService locks)
    {
        _history = history;
        _settings = settings;
        _lifetime = lifetime;
        _appSettings = appSettings;
        _locks = locks;

        InitializeComponent();
        Title = Tr.Get("App_Name");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.ApplyCultureDirection();
        theme.Attach(this);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(940, 660));

        // Lock-on-minimize: hide unlocked categories again when the window goes to the taskbar.
        AppWindow.Changed += (_, args) =>
        {
            if (!args.DidPresenterChange) return;
            if (_appSettings.Current.LockOnMinimize &&
                AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
                _locks.Reset();
        };

        // Closing only hides the window; the app keeps running in the tray.
        AppWindow.Closing += (_, args) =>
        {
            if (_lifetime.IsExiting) return;
            args.Cancel = true;
            AppWindow.Hide();
        };

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
        Nav.SelectedItem = Nav.FooterMenuItems[0];
        AppWindow.Show();
        Activate();
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        var isSettings = (item.Tag as string) == "settings";
        SettingsSearchBox.Visibility = isSettings ? Visibility.Visible : Visibility.Collapsed;
        if (!isSettings) SettingsSearchBox.Text = string.Empty;

        if (isSettings)
        {
            ContentFrame.Content = _settings.Value;
            _ = _settings.Value.LoadAsync();
            return;
        }

        var page = _history.Value;
        ContentFrame.Content = page;
        _ = page.LoadAsync();
    }

    private void OnSettingsSearchChanged(object sender, TextChangedEventArgs e) =>
        _settings.Value.ViewModel.Filter = SettingsSearchBox.Text;
}
