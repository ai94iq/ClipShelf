using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClipShelf.Desktop.Features;
using ClipShelf.Desktop.Platform.Linux;

namespace ClipShelf.Desktop.Views;

// The Linux twin of FlyoutWindow: same view, same shared actions, X11 frame
// instead of the Win32 one. No foreground hook: losing focus (Deactivated)
// hides the flyout, which is all the auto-dismiss needs on X11.
[SupportedOSPlatform("linux")]
public sealed partial class LinuxFlyoutWindow : Window, IFlyoutHost
{
    private ClipboardPanelViewModel? _viewModel;
    private FlyoutActions? _actions;
    private bool _shown;

    // Parameterless constructor for XAML designer/loader
    public LinuxFlyoutWindow() => InitializeComponent();

    public LinuxFlyoutWindow(ClipboardPanelViewModel viewModel, IPasteBack pasteBack)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Title = Tr.Get("App_Name");

        _actions = new FlyoutActions(this, viewModel, pasteBack);

        // Tunneling, so arrows, Enter and Esc work while the search box holds focus.
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => HideFlyout();

        // Show it once, out of sight. From then on "hidden" means parked
        // off-screen, keeping the surface alive for the next open.
        X11PanelFrame.MoveOffscreen(this);
        Show();
        _viewModel.IsViewVisible = false;
    }

    public IntPtr PreviousWindowHandle => IntPtr.Zero;

    Control IFlyoutHost.Anchor => this;

    public void Toggle()
    {
        if (_shown) HideFlyout();
        else ShowFlyout();
    }

    public void HideFlyout()
    {
        if (!_shown) return;

        _shown = false;
        _viewModel!.IsViewVisible = false;
        X11PanelFrame.MoveOffscreen(this);
        Hide();
    }

    private void ShowFlyout()
    {
        _viewModel!.ResetTransientState();
        _viewModel.IsViewVisible = true;
        _shown = true;
        X11PanelFrame.PositionNearCursor(this);
        Show();
        Activate();
        SearchBox.Focus();

        // Refresh after the window is on screen so opening is never delayed; the quiet refresh
        // keeps the previous list visible instead of showing a spinner.
        _ = _viewModel.RefreshQuietAsync();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e) => _actions!.OnKeyDown(e);

    private void OnClipListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left) return;
        if (e.Source is Button) return;
        if (ItemAt(e.Source) is { } item) _actions!.ActivateItem(item);
    }

    private void OnClipListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button) return;
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        if (ItemAt(e.Source) is not { } item) return;

        e.Handled = true;
        _ = _actions!.OpenCategoryMenuAsync(item);
    }

    private static ClipItemViewModel? ItemAt(object? source) =>
        (source as Control)?.DataContext as ClipItemViewModel;
}
