using ClipShelf.App.Shell;
using Microsoft.UI.Xaml.Controls;

namespace ClipShelf.App.Features.Settings;

public sealed partial class SettingsPage : UserControl
{
    private readonly Lazy<AppShellService> _shell;

    public SettingsPage(SettingsViewModel viewModel, Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;
        _shell = shell;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnExit(object sender, RoutedEventArgs e) => _shell.Value.Exit();
}
