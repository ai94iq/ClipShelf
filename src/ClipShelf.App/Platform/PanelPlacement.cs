namespace ClipShelf.App.Platform;

// Pure math behind the flyout's placement, kept apart so it can be tested.
public static class PanelPlacement
{
    // The flyout sits below-right of the pointer, pushed back inside the work area at the edges
    // (so a click on the tray icon, where the pointer sits on the taskbar, still tucks into the corner).
    public static (int X, int Y) NearCursor(
        int cursorX,
        int cursorY,
        int width,
        int height,
        int workLeft,
        int workTop,
        int workRight,
        int workBottom,
        int margin,
        int gap)
    {
        var x = Math.Clamp(cursorX + gap, workLeft + margin, workRight - margin - width);
        var y = Math.Clamp(cursorY + gap, workTop + margin, workBottom - margin - height);
        return (x, y);
    }
}
