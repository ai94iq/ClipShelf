using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;

namespace ClipShelf.Desktop.Platform.Windows;

// The flyout's window chrome: borderless, topmost, out of the taskbar, rounded, frameless. The
// Avalonia window declares the borderless and topmost flags; this class strips the Win32 frame
// styles the window class adds and positions the window near the cursor.
[SupportedOSPlatform("windows")]
internal static class PanelFrame
{
    private const int WidthLogical = 360;
    private const int HeightLogical = 480;
    private const int MarginLogical = 12;
    private const int CursorGapLogical = 8;
    private const int OffscreenCoordinate = -32000;

    public static void Configure(Window window)
    {
        var handle = HandleOf(window);
        if (handle == IntPtr.Zero) return;

        // Out of the taskbar and Alt+Tab; strip the frame styles the window class adds
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

        var corner = NativeMethods.DwmWindowCornerRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));

        var border = NativeMethods.DwmColorNone;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowBorderColor, ref border, sizeof(int));
    }

    // Anchor the flyout next to the mouse pointer, kept inside the monitor's work area.
    public static void PositionNearCursor(Window window)
    {
        var handle = HandleOf(window);
        if (handle == IntPtr.Zero) return;

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

        window.Position = new PixelPoint(x, y);
    }

    // Keeps the window alive out of sight instead of hiding it: Windows re-creates the surface of
    // a hidden acrylic window on the next show, which flashes black.
    public static void MoveOffscreen(Window window) =>
        window.Position = new PixelPoint(OffscreenCoordinate, OffscreenCoordinate);

    // Windows refuses SetForegroundWindow while another app owns the foreground (a hotkey or a
    // tray click are not enough), so tap ALT first, the standard way to lift that restriction.
    public static void ForceForeground(Window window)
    {
        var handle = HandleOf(window);
        if (handle == IntPtr.Zero || NativeMethods.GetForegroundWindow() == handle) return;

        NativeMethods.keybd_event(NativeMethods.VirtualKeyMenu, 0, 0, UIntPtr.Zero);
        NativeMethods.SetForegroundWindow(handle);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyMenu, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
    }

    // Give the foreground back to the app the user came from, so keystrokes do not land on the
    // parked window.
    public static void RestoreForeground(Window window, IntPtr previous)
    {
        var handle = HandleOf(window);
        if (previous != IntPtr.Zero && NativeMethods.GetForegroundWindow() == handle)
            NativeMethods.SetForegroundWindow(previous);
    }

    private static IntPtr HandleOf(Window window) =>
        window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
}
