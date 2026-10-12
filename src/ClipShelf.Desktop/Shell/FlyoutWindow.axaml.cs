using Avalonia;
using Avalonia.Controls;
using ClipShelf.Desktop.Platform;

namespace ClipShelf.Desktop.Shell;

// The borderless, topmost flyout next to the pointer. It hides when it loses focus or on Esc,
// and hands the foreground back to the app the user came from.
public sealed partial class FlyoutWindow : Window
{
    private const int WidthDip = 360;
    private const int HeightDip = 480;
    private const int MarginDip = 12;
    private const int CursorGapDip = 8;

    // A tray click deactivates the open flyout (hiding it) just before it toggles; a toggle this
    // soon after such a hide is that same click and must not reopen it.
    private static readonly TimeSpan DeactivationEcho = TimeSpan.FromMilliseconds(300);

    private readonly IFlyoutPlatform _platform;
    private readonly IPasteBack _pasteBack;
    private readonly TimeProvider _time;
    private IntPtr _previousWindow;
    private DateTimeOffset? _hiddenByDeactivationAt;

    public FlyoutWindow(
        ClipboardPanelViewModel viewModel, IFlyoutPlatform platform, IPasteBack pasteBack, TimeProvider time)
    {
        ViewModel = viewModel;
        _platform = platform;
        _pasteBack = pasteBack;
        _time = time;
        InitializeComponent();
        DataContext = viewModel;
        FlowDirection = Culture.IsRtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
        Deactivated += (_, _) => HideAfterDeactivation();
    }

    public ClipboardPanelViewModel ViewModel { get; }

    public bool IsOpen { get; private set; }

    internal IntPtr PreviousWindow => _previousWindow;

    // Pixel bounds next to the pointer, pushed back inside the work area at the edges.
    public static PixelRect Place(PixelPoint cursor, PixelRect workArea, double scaling)
    {
        int Px(int dip) => (int)(dip * scaling);
        var (x, y) = PanelPlacement.NearCursor(
            cursor.X, cursor.Y, Px(WidthDip), Px(HeightDip),
            workArea.X, workArea.Y, workArea.Right, workArea.Bottom, Px(MarginDip), Px(CursorGapDip));
        return new PixelRect(x, y, Px(WidthDip), Px(HeightDip));
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            HideFlyout();
            return;
        }

        if (_hiddenByDeactivationAt is { } hiddenAt && _time.GetUtcNow() - hiddenAt < DeactivationEcho)
        {
            _hiddenByDeactivationAt = null;
            return;
        }

        ShowFlyout();
    }

    public void ShowFlyout()
    {
        ViewModel.ResetTransientState();
        ViewModel.IsViewVisible = true;
        _previousWindow = _platform.ForegroundWindow();
        _hiddenByDeactivationAt = null;
        IsOpen = true;

        var cursor = _platform.Cursor();
        if ((Screens.ScreenFromPoint(cursor) ?? Screens.Primary) is { } screen)
            Position = Place(cursor, screen.WorkingArea, screen.Scaling).Position;

        Show();
        Activate();
        _platform.ForceForeground(TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);

        // Refresh after the window is on screen so opening is never delayed; the quiet refresh
        // keeps the previous list visible instead of showing a spinner.
        _ = ViewModel.RefreshQuietAsync();
    }

    public void HideFlyout()
    {
        if (!IsOpen) return;

        IsOpen = false;
        ViewModel.IsViewVisible = false;
        _platform.RestoreForeground(TryGetPlatformHandle()?.Handle ?? IntPtr.Zero, _previousWindow);
        Hide();
    }

    internal void HideAfterDeactivation()
    {
        if (!IsOpen) return;

        HideFlyout();
        _hiddenByDeactivationAt = _time.GetUtcNow();
    }
}
