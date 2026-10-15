using Avalonia.Controls;
using ClipShelf.Desktop.Features;

namespace ClipShelf.Desktop.Shell;

// The app shell: History and Settings behind a compact rail. Closing it only hides it.
public sealed partial class MainWindow : Window
{
    private readonly AppLifetime _lifetime;
    private readonly Lazy<HistoryPage>? _history;
    private readonly Lazy<SettingsPage>? _settings;

    public MainWindow(AppLifetime lifetime, Lazy<HistoryPage>? history = null, Lazy<SettingsPage>? settings = null, bool? nativeCaption = null)
    {
        _lifetime = lifetime;
        _history = history;
        _settings = settings;
        InitializeComponent();
        WindowSurface.Attach(this, "ClipShelfCanvasBrush");

        // macOS keeps its own traffic lights (top left) over the extended title row; ours are the
        // Windows-style buttons and would be a second, foreign set there.
        if (nativeCaption ?? OperatingSystem.IsMacOS())
        {
            WindowDecorations = WindowDecorations.Full;
            CaptionButtons.IsVisible = false;
            AppTitle.Margin = new Avalonia.Thickness(80, 0, 0, 0);
        }
        FlowDirection = Culture.IsRtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;

        HistoryButton.IsCheckedChanged += (_, _) => { if (HistoryButton.IsChecked == true) Select(settings: false); };
        SettingsButton.IsCheckedChanged += (_, _) => { if (SettingsButton.IsChecked == true) Select(settings: true); };

        // Closing only hides the window; the app keeps running in the tray.
        // The search box filters the Settings groups.
        SearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty && _settings is { IsValueCreated: true })
                _settings.Value.ViewModel.Filter = SearchBox.Text ?? string.Empty;
        };

        // Mouse clicks on the caption buttons go to the OS through their roles; these handle
        // keyboard and screen-reader activation.
        MinimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        MaximizeButton.Click += (_, _) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        CloseButton.Click += (_, _) => Close();
        PropertyChanged += (_, e) =>
        {
            if (e.Property != WindowStateProperty) return;
            MaximizeGlyph.IsVisible = WindowState != WindowState.Maximized;
            RestoreGlyph.IsVisible = WindowState == WindowState.Maximized;
        };

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
        if (settings && _settings is not null)
        {
            Section.Content = _settings.Value;
            _settings.Value.ViewModel.Filter = SearchBox.Text ?? string.Empty;
            _ = _settings.Value.LoadAsync();
            return;
        }

        Section.Content = null;
    }

    // Out of view, the history view model skips background refreshes.
    private void MarkHistoryVisible(bool visible)
    {
        if (_history is { IsValueCreated: true }) _history.Value.ViewModel.IsViewVisible = visible;
    }
}
