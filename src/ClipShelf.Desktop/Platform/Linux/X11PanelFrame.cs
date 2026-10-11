using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;

namespace ClipShelf.Desktop.Platform.Linux;

// The Linux side of the flyout frame: park off-screen while hidden, anchor
// next to the cursor on show. Placement math is the shared PanelPlacement
// helper; only the cursor query and the screen bounds are platform-specific.
// Short display connections per call: positioning happens on user gestures.
[SupportedOSPlatform("linux")]
internal static class X11PanelFrame
{
    private const int WidthLogical = 380;
    private const int HeightLogical = 520;
    private const int MarginLogical = 12;
    private const int CursorGapLogical = 8;
    private const int OffscreenCoordinate = -32000;

    internal static void MoveOffscreen(Window window) =>
        window.Position = new PixelPoint(OffscreenCoordinate, OffscreenCoordinate);

    internal static void PositionNearCursor(Window window)
    {
        if (!TryCursor(out var cursorX, out var cursorY)) return;

        var area = window.Screens.Primary?.WorkingArea
            ?? new PixelRect(0, 0, 1920, 1080);
        var scale = window.Screens.Primary?.Scaling ?? 1.0;
        var width = (int)(WidthLogical * scale);
        var height = (int)(HeightLogical * scale);
        var margin = (int)(MarginLogical * scale);
        var gap = (int)(CursorGapLogical * scale);

        var (x, y) = ClipShelf.Presentation.Platform.PanelPlacement.NearCursor(
            cursorX, cursorY, width, height,
            area.X, area.Y, area.X + area.Width, area.Y + area.Height, margin, gap);
        window.Position = new PixelPoint(x, y);
    }

    private static bool TryCursor(out int x, out int y)
    {
        x = 0;
        y = 0;
        try
        {
            var display = X11Native.XOpenDisplay(null);
            if (display == IntPtr.Zero) return false;
            try
            {
                var root = X11Native.XDefaultRootWindow(display);
                var ok = X11Native.XQueryPointer(display, root, out _, out _,
                    out x, out y, out _, out _, out _);
                return ok != 0;
            }
            finally
            {
                X11Native.XCloseDisplay(display);
            }
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }
}
