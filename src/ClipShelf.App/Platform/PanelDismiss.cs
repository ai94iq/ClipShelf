namespace ClipShelf.App.Platform;

// Decides whether a foreground change should dismiss the flyout.
public static class PanelDismiss
{
    // Clicking the taskbar must not dismiss it: a tray click toggles the flyout itself, and
    // hiding through this path would flip it back open.
    public static bool ShouldHide(
        IntPtr flyoutWindow, IntPtr foregroundWindow, string? foregroundClass, bool isVisible)
    {
        var taskbar = foregroundClass is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow";
        return isVisible
            && foregroundWindow != IntPtr.Zero
            && foregroundWindow != flyoutWindow
            && !taskbar;
    }
}
