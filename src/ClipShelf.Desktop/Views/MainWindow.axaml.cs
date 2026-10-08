using Avalonia.Controls;

namespace ClipShelf.Desktop.Views;

// The app shell: the committed rail on the start edge and a content host. The History and
// Settings pages arrive in plan 2b; placeholder panels stand in until then.
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, e) =>
        {
            e.Cancel = true;    // closing only hides the window, like the WinUI app
            Hide();
        };
        ShowHistory();
    }

    public void ShowHistory() => ShowPage("History");

    public void ShowSettings() => ShowPage("Settings");

    private void ShowPage(string title)
    {
        ContentArea.Content = new TextBlock
        {
            Text = title,
            Classes = { "grid-title" },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        if (!IsVisible) Show();
        Activate();
    }

    private void OnHistoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ShowHistory();

    private void OnSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ShowSettings();
}
