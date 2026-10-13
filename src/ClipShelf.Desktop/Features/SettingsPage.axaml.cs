using Avalonia.Controls;
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

    public Task LoadAsync() => Task.WhenAll(ViewModel.RefreshCategoriesAsync(), ViewModel.CheckForUpdatesOnceAsync());
}
