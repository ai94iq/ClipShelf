using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace ClipShelf.App.Platform;

// The flyout's window chrome: borderless, topmost, out of the taskbar, rounded, frameless.
internal static class PanelFrame
{
    private const int WidthLogical = 360;
    private const int HeightLogical = 480;
    private const int MarginLogical = 12;
    private const int CursorGapLogical = 8;
    private const int OffscreenCoordinate = -32000;

    public static void Configure(Window window)
    {
        // Configure the existing presenter; replacing it (SetPresenter) drops the SystemBackdrop
        // and the flyout would render opaque.
        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Out of the taskbar and Alt+Tab; strip the frame styles WinUI's window class adds
        // (WS_EX_WINDOWEDGE, WS_CAPTION, WS_THICKFRAME are the light 1px border).
        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        exStyle = (exStyle | NativeMethods.WsExToolWindow) & ~NativeMethods.WsExWindowEdge & ~NativeMethods.WsExClientEdge;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(exStyle));

        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlStyle).ToInt64();
        style &= ~(NativeMethods.WsCaption | NativeMethods.WsThickFrame | NativeMethods.WsBorder);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlStyle, new IntPtr(style));

        NativeMethods.SetWindowPos(
            handle, IntPtr.Zero, 0, 0, 0, 0,
            NativeMethods.SwpFrameChanged | NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder);

        ApplyTheme(window);

        // DWM restores the default frame when the window activates, so re-apply the theme then.
        window.Activated += (_, args) =>
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated) ApplyTheme(window);
        };
    }

    // Anchor the flyout next to the mouse pointer, kept inside the monitor's work area.
    public static void PositionNearCursor(Window window)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = (int)(WidthLogical * scale);
        var height = (int)(HeightLogical * scale);
        var margin = (int)(MarginLogical * scale);
        var gap = (int)(CursorGapLogical * scale);

        NativeMethods.GetCursorPos(out var cursor);
        var monitor = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { CbSize = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return;

        var (x, y) = PanelPlacement.NearCursor(
            cursor.X, cursor.Y, width, height,
            info.Work.Left, info.Work.Top, info.Work.Right, info.Work.Bottom, margin, gap);
        var bounds = new Windows.Graphics.RectInt32(x, y, width, height);
        var position = window.AppWindow.Position;
        var size = window.AppWindow.Size;
        if (position.X != bounds.X || position.Y != bounds.Y ||
            size.Width != bounds.Width || size.Height != bounds.Height)
            window.AppWindow.MoveAndResize(bounds);
    }

    // Keeps the window alive out of sight instead of hiding it: Windows re-creates the surface of
    // a hidden acrylic window on the next show, which flashes black.
    public static void MoveOffscreen(Window window)
    {
        var size = window.AppWindow.Size;
        window.AppWindow.MoveAndResize(
            new Windows.Graphics.RectInt32(OffscreenCoordinate, OffscreenCoordinate, size.Width, size.Height));
    }

    // Windows refuses SetForegroundWindow while another app owns the foreground (a hotkey or a
    // tray click are not enough), so tap ALT first, the standard way to lift that restriction.
    public static void ForceForeground(Window window)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (NativeMethods.GetForegroundWindow() == handle) return;

        NativeMethods.keybd_event(NativeMethods.VirtualKeyMenu, 0, 0, UIntPtr.Zero);
        NativeMethods.SetForegroundWindow(handle);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyMenu, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
    }

    // Give the foreground back to the app the user came from, so keystrokes do not land on the
    // parked window.
    public static void RestoreForeground(Window window, IntPtr previous)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (previous != IntPtr.Zero && NativeMethods.GetForegroundWindow() == handle)
            NativeMethods.SetForegroundWindow(previous);
    }

    // DWM restores the default frame when the window activates, so this runs again on activation.
    public static void ApplyTheme(Window window) =>
        SetDarkFrame(window, (window.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark);

    // The caption buttons follow the immersive-dark flag, so a white theme needs it off even when
    // the system itself is dark.
    public static void SetDarkFrame(Window window, bool dark)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var value = dark ? 1 : 0;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmImmersiveDarkMode, ref value, sizeof(int));

        var corner = NativeMethods.DwmWindowCornerRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));

        var border = NativeMethods.DwmColorNone;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowBorderColor, ref border, sizeof(int));

        // On a custom title bar the buttons are coloured here; the DWM flag alone leaves them white.
        var titleBar = window.AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = dark ? Colors.White : Colors.Black;
        titleBar.ButtonInactiveForegroundColor = dark ? Colors.Gray : Colors.DimGray;
        titleBar.ButtonHoverBackgroundColor = dark
            ? Windows.UI.Color.FromArgb(24, 255, 255, 255)
            : Windows.UI.Color.FromArgb(24, 0, 0, 0);
        titleBar.ButtonHoverForegroundColor = dark ? Colors.White : Colors.Black;
        titleBar.ButtonPressedBackgroundColor = dark
            ? Windows.UI.Color.FromArgb(48, 255, 255, 255)
            : Windows.UI.Color.FromArgb(48, 0, 0, 0);
        titleBar.ButtonPressedForegroundColor = dark ? Colors.White : Colors.Black;
    }
}
