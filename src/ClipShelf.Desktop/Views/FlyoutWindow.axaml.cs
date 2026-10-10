using System.Runtime.Versioning;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClipShelf.Desktop.Features;
using ClipShelf.Desktop.Platform.Windows;

namespace ClipShelf.Desktop.Views;

// A borderless, always-on-top flyout next to the mouse pointer: the world's first viewport. It
// hides when it loses focus or the user presses Esc. Window work from inside the Win32 foreground
// hook is deferred through the dispatcher; doing it inline once crashed the WinUI build.
[SupportedOSPlatform("windows")]
public sealed partial class FlyoutWindow : Window
{
    private readonly ClipboardPanelViewModel _viewModel;
    private readonly FlyoutActions _actions;
    private readonly NativeMethods.WinEventProc _foregroundChanged;
    private readonly IntPtr _handle;
    private IntPtr _previousWindow = IntPtr.Zero;
    private bool _shown;

    public FlyoutWindow(ClipboardPanelViewModel viewModel, IPasteBack pasteBack)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Title = Tr.Get("App_Name");

        _actions = new FlyoutActions(this, viewModel, pasteBack);

        // Tunneling, so arrows, Enter and Esc work while the search box holds focus.
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

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
        // the surface from being re-created (and flashing) on the next open. The platform handle
        // exists only after the first Show, so the frame work waits for it.
        PanelFrame.MoveOffscreen(this);
        Show();
        _handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        PanelFrame.Configure(this);
        _viewModel.IsViewVisible = false;
    }

    internal IntPtr PreviousWindowHandle => _previousWindow;

    public void Toggle()
    {
        if (_shown) HideFlyout();
        else ShowFlyout();
    }

    public void HideFlyout()
    {
        if (!_shown) return;

        _shown = false;
        _viewModel.IsViewVisible = false;
        PanelFrame.RestoreForeground(this, _previousWindow);
        PanelFrame.MoveOffscreen(this);
    }

    private void ShowFlyout()
    {
        _viewModel.ResetTransientState();
        _viewModel.IsViewVisible = true;
        _previousWindow = NativeMethods.GetForegroundWindow();
        _shown = true;
        PanelFrame.PositionNearCursor(this);
        Show();
        Activate();
        PanelFrame.ForceForeground(this);
        SearchBox.Focus();

        // Refresh after the window is on screen so opening is never delayed; the quiet refresh
        // keeps the previous list visible instead of showing a spinner.
        _ = _viewModel.RefreshQuietAsync();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e) => _actions.OnKeyDown(e);

    private void OnClipListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left) return;
        if (ItemAt(e.Source) is { } item) _actions.ActivateItem(item);
    }

    private void OnClipListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        if (ItemAt(e.Source) is not { } item) return;

        e.Handled = true;
        _ = _actions.OpenCategoryMenuAsync(item);
    }

    private static ClipItemViewModel? ItemAt(object? source) =>
        (source as Control)?.DataContext as ClipItemViewModel;

    private void OnForegroundChanged(
        IntPtr hook, uint eventType, IntPtr window, int idObject, int idChild, uint thread, uint time)
    {
        var className = new StringBuilder(64);
        NativeMethods.GetClassName(window, className, className.Capacity);
        if (!PanelDismiss.ShouldHide(_handle, window, className.ToString(), _shown)) return;

        // Window work from inside a Win32 hook callback re-enters the UI while it is mid-activation
        // and can take the process down with an access violation; hand it to the next dispatcher
        // pass instead.
        Dispatcher.UIThread.Post(HideFlyout);
    }
}
