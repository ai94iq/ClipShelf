using Avalonia.Controls;
using Avalonia.Interactivity;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Features;

// The Settings section of the main window. Every change saves and applies at once; the view
// model owns that, the page only adds what needs the UI (pickers, the hotkey recorder).
public sealed partial class SettingsPage : UserControl
{
    private readonly Lazy<AppShellService> _shell;

    public SettingsPage(SettingsViewModel viewModel, Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;
        _shell = shell;
        InitializeComponent();
        DataContext = viewModel;
    }

    public SettingsViewModel ViewModel { get; }

    private void OnRestartClick(object? sender, RoutedEventArgs e) => _shell.Value.Restart();

    private void OnAccentCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true, DataContext: AccentOption option } && ViewModel.SelectedAccent != option)
            ViewModel.SelectedAccent = option;
    }

    public Task LoadAsync() => Task.WhenAll(ViewModel.RefreshCategoriesAsync(), ViewModel.CheckForUpdatesOnceAsync());
}
