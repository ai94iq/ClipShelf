using Avalonia.Controls;
using Avalonia.Media;
using ClipShelf.Desktop.Features;

namespace ClipShelf.Desktop.Shell;

// The app shell: History and Settings behind a compact rail. Closing it only hides it.
public sealed partial class MainWindow : Window
{
    private readonly AppLifetime _lifetime;
    private readonly Lazy<HistoryPage>? _history;

    public MainWindow(AppLifetime lifetime, Lazy<HistoryPage>? history = null)
    {
        _lifetime = lifetime;
        _history = history;
        InitializeComponent();
        FlowDirection = Culture.IsRtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;

        HistoryButton.IsCheckedChanged += (_, _) => { if (HistoryButton.IsChecked == true) Select(settings: false); };
        SettingsButton.IsCheckedChanged += (_, _) => { if (SettingsButton.IsChecked == true) Select(settings: true); };

        // Closing only hides the window; the app keeps running in the tray.
        Closing += (_, args) =>
        {
            if (_lifetime.IsExiting) return;
            args.Cancel = true;
            Hide();
            MarkHistoryVisible(false);
        };
    }

    public bool IsSettings => SettingsButton.IsChecked == true;

    public bool IsSearchVisible => SearchBox.IsVisible;

    public string SearchText
    {
        get => SearchBox.Text ?? string.Empty;
        set => SearchBox.Text = value;
    }

    // Every show reloads the section, as main does, so reopening the window is never stale.
    public void ShowHistory()
    {
        HistoryButton.IsChecked = true;
        Select(settings: false);
        Bring();
    }

    public void ShowSettings()
    {
        SettingsButton.IsChecked = true;
        Select(settings: true);
        Bring();
    }

    private void Bring()
    {
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show();
        Activate();
    }

    private void Select(bool settings)
    {
        SearchBox.IsVisible = settings;
        if (!settings) SearchBox.Text = string.Empty;

        if (!settings && _history is not null)
        {
            Section.Content = _history.Value;
            MarkHistoryVisible(true);
            _ = _history.Value.LoadAsync();
            return;
        }

        MarkHistoryVisible(false);
        // ponytail: Settings shows its title until the real page lands (batch 6).
        Section.Content = new TextBlock
        {
            Text = Tr.Get(settings ? "Nav_Settings" : "History_Title"),
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
        };
    }

    // Out of view, the history view model skips background refreshes.
    private void MarkHistoryVisible(bool visible)
    {
        if (_history is { IsValueCreated: true }) _history.Value.ViewModel.IsViewVisible = visible;
    }
}
