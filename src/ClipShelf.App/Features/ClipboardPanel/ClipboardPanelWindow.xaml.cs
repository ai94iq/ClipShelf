using System.Text;
using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ClipShelf.App.Features.ClipboardPanel;

// A borderless, always-on-top flyout next to the mouse pointer, styled like the
// Windows 11 clipboard flyout. It hides when it loses focus or the user presses Esc.
public sealed partial class ClipboardPanelWindow : Window
{
    private readonly ClipboardPanelActions _actions;
    private readonly NativeMethods.WinEventProc _foregroundChanged;
    private readonly IntPtr _handle;
    private IntPtr _previousWindow = IntPtr.Zero;
    private bool _shown;

    public ClipboardPanelWindow(
        ClipboardPanelViewModel viewModel,
        Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;

        InitializeComponent();
        Root.DataContext = viewModel;
        Title = Tr.Get("App_Name");
        this.ApplyCultureDirection();

        // The search box's clear button sits at the end edge; keep its inset on that side in RTL.
        SearchBox.Padding = Culture.IsRtl ? new Thickness(8, 0, 0, 0) : new Thickness(0, 0, 8, 0);
        DestructiveHover.TintOnHover(ClearAllButton);
        DestructiveHover.TintOnHover(ConfirmClearButton);

        _actions = new ClipboardPanelActions(this, viewModel, shell);
        _handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        PanelFrame.Configure(this);
        WindowSurface.ApplyAcrylic(this, Root);

        // Clicking anywhere outside should dismiss the flyout, which - unlike an activation event -
        // also covers opens where Windows never made it the active window (hotkey or tray click).
        _foregroundChanged = OnForegroundChanged;
        NativeMethods.SetWinEventHook(
            NativeMethods.EventSystemForeground,
            NativeMethods.EventSystemForeground,
            IntPtr.Zero,
            _foregroundChanged,
            0,
            0,
            NativeMethods.WineventOutOfContext);

        // Show it once, out of sight. From then on "hidden" means parked off-screen, which keeps
        // Windows from re-creating the acrylic surface and flashing black on the next open.
        PanelFrame.MoveOffscreen(this);
        AppWindow.Show();
    }

    public ClipboardPanelViewModel ViewModel { get; }
    internal IntPtr PreviousWindowHandle => _previousWindow;

    // Where a row's category menu should appear.
    internal FrameworkElement RowAnchor(ClipItemViewModel item) =>
        ClipList.ContainerFromItem(item) as FrameworkElement ?? Root;

    public Task ToggleAsync()
    {
        if (_shown)
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
        _shown = true;
        PanelFrame.PositionNearCursor(this);
        Activate();
        PanelFrame.ForceForeground(this);
        SearchBox.Focus(FocusState.Programmatic);

        // Refresh after the window is on screen so opening is never delayed; the quiet
        // refresh keeps the previous list visible instead of showing a spinner.
        _ = ViewModel.RefreshQuietAsync();
        return Task.CompletedTask;
    }

    public void Hide()
    {
        if (!_shown) return;

        _shown = false;
        PanelFrame.RestoreForeground(this, _previousWindow);
        PanelFrame.MoveOffscreen(this);
    }

    private void OnForegroundChanged(
        IntPtr hook, uint eventType, IntPtr window, int idObject, int idChild, uint thread, uint time)
    {
        var className = new StringBuilder(64);
        NativeMethods.GetClassName(window, className, className.Capacity);
        if (PanelDismiss.ShouldHide(_handle, window, className.ToString(), _shown)) Hide();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e) => _actions.OnItemClick(e);

    // Loads the next page once the list realizes its last row (the user scrolled to the end).
    private void OnClipListContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase != 0 || args.InRecycleQueue) return;
        if (args.Item is not ClipItemViewModel item) return;
        if (!ReferenceEquals(item, ViewModel.LastVisibleItem)) return;

        if (ViewModel.LoadMoreCommand.CanExecute(null)) ViewModel.LoadMoreCommand.Execute(null);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e) => _actions.OnKeyDown(e);
}
