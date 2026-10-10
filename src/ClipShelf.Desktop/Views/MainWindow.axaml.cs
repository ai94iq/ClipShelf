using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ClipShelf.Desktop.Views;

// The app shell: the committed rail on the start edge and a content host swapping the
// History and Settings pages. Closing only hides the window; the app lives in the tray.
public sealed partial class MainWindow : Window
{
    private readonly Lazy<HistoryPage> _history;
    private readonly Lazy<SettingsPage> _settings;

    public MainWindow(Lazy<HistoryPage> history, Lazy<SettingsPage> settings)
    {
        _history = history;
        _settings = settings;
        InitializeComponent();
        Title = Tr.Get("App_Name");
        ApplyCulture();
        SetAppIcon();
        Closing += (_, e) =>
        {
            e.Cancel = true;    // closing only hides the window, like the WinUI app
            Hide();
        };
        ShowHistory();
    }

    // Rail navigation: swap the page into the content host and load it. Only the visible
    // history page handles the shared view model's menu requests (existing TrayController
    // calls land here).
    public void ShowHistory()
    {
        var page = _history.Value;
        ContentArea.Content = page;
        page.IsActive = true;
        if (!IsVisible) Show();
        Activate();
        _ = page.LoadAsync();
    }

    public void ShowSettings()
    {
        if (_history.IsValueCreated) _history.Value.IsActive = false;
        var page = _settings.Value;
        ContentArea.Content = page;
        if (!IsVisible) Show();
        Activate();
        _ = page.LoadAsync();
    }

    private void OnHistoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ShowHistory();

    private void OnSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ShowSettings();

    private void ApplyCulture()
    {
        FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        if (Culture.IsRtl)
            FontFamily = new FontFamily("avares://ClipShelf/Assets/Fonts/NotoSansArabic-Regular.ttf#Noto Sans Arabic");
    }

    private void SetAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(path)) Icon = new WindowIcon(path);
    }
}
