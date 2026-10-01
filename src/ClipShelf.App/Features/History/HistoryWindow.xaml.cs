using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Shell;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Features.History;

// The full history app window, opened from the tray or the flyout. Clicking an item copies it
// back to the clipboard.
public sealed partial class HistoryWindow : Window
{
    private readonly AppLifetime _lifetime;

    public HistoryWindow(ClipboardPanelViewModel viewModel, IThemeService theme, AppLifetime lifetime)
    {
        ViewModel = viewModel;
        _lifetime = lifetime;

        InitializeComponent();
        Root.DataContext = viewModel;
        Title = Tr.Get("App_Name");
        this.ApplyCultureDirection();
        theme.Attach(this);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(760, 560));

        // Closing only hides the window; the app keeps running in the tray.
        AppWindow.Closing += (_, args) =>
        {
            if (_lifetime.IsExiting) return;
            args.Cancel = true;
            AppWindow.Hide();
        };

        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop();
        else
            Root.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
    }

    public ClipboardPanelViewModel ViewModel { get; }

    public async Task ShowAsync()
    {
        AppWindow.Show();
        Activate();
        await ViewModel.RefreshAsync();
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) ViewModel.Activate(item);
    }
}
