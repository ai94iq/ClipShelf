using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ClipShelf.App.Features.ClipboardPanel;

// A borderless, always-on-top flyout at the bottom-right of the active monitor, styled like the
// Windows 11 clipboard flyout. It hides when it loses focus or the user presses Esc.
public sealed partial class ClipboardPanelWindow : Window
{
    private readonly ClipboardPanelActions _actions;
    private IntPtr _previousWindow = IntPtr.Zero;

    public ClipboardPanelWindow(
        ClipboardPanelViewModel viewModel,
        SettingsService settings,
        Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;

        InitializeComponent();
        Root.DataContext = viewModel;
        Title = Tr.Get("App_Name");
        this.ApplyCultureDirection();
        DestructiveHover.TintOnHover(ClearAllButton);
        DestructiveHover.TintOnHover(ConfirmClearButton);

        _actions = new ClipboardPanelActions(this, viewModel, settings, shell);
        PanelFrame.Configure(this);
        WindowSurface.ApplyAcrylic(this, Root);
        PanelFrame.WatchActivation(this, Hide);
    }

    public ClipboardPanelViewModel ViewModel { get; }
    internal IntPtr PreviousWindowHandle => _previousWindow;

    // Where a row's category menu should appear.
    internal FrameworkElement RowAnchor(ClipItemViewModel item) =>
        ClipList.ContainerFromItem(item) as FrameworkElement ?? Root;

    public Task ToggleAsync()
    {
        if (AppWindow.IsVisible)
        {
            Hide();
            return Task.CompletedTask;
        }

        return ShowAsync();
    }

    public Task ShowAsync()
    {
        ViewModel.ResetTransientState();
        _previousWindow = NativeMethods.GetForegroundWindow();
        PanelFrame.PositionBottomRightOfCursor(this);
        AppWindow.Show();
        Activate();
        SearchBox.Focus(FocusState.Programmatic);

        // Refresh after the window is on screen so opening is never delayed; the quiet
        // refresh keeps the previous list visible instead of showing a spinner.
        _ = ViewModel.RefreshQuietAsync();
        return Task.CompletedTask;
    }

    public void Hide() => AppWindow.Hide();

    private void OnItemClick(object sender, ItemClickEventArgs e) => _actions.OnItemClick(e);

    private void OnKeyDown(object sender, KeyRoutedEventArgs e) => _actions.OnKeyDown(e);
}
