using Avalonia.Controls;
using Avalonia.Media;

namespace ClipShelf.Desktop.Shell;

// The app shell: History and Settings behind a compact rail. Closing it only hides it.
public sealed partial class MainWindow : Window
{
    private readonly AppLifetime _lifetime;

    public MainWindow(AppLifetime lifetime)
    {
        _lifetime = lifetime;
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
        };
    }

    public bool IsSettings => SettingsButton.IsChecked == true;

    public bool IsSearchVisible => SearchBox.IsVisible;

    public string SearchText
    {
        get => SearchBox.Text ?? string.Empty;
        set => SearchBox.Text = value;
    }

    public void ShowHistory()
    {
        HistoryButton.IsChecked = true;
        Bring();
    }

    public void ShowSettings()
    {
        SettingsButton.IsChecked = true;
        Bring();
    }

    private void Bring()
    {
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show();
        Activate();
    }

    // ponytail: the section shows its title until the real pages land (batches 5 and 6).
    private void Select(bool settings)
    {
        SearchBox.IsVisible = settings;
        if (!settings) SearchBox.Text = string.Empty;

        Section.Content = new TextBlock
        {
            Text = Tr.Get(settings ? "Nav_Settings" : "History_Title"),
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
        };
    }
}
