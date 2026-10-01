namespace ClipShelf.App.Platform;

// Sends Ctrl+V to the window the user was in before the flyout opened.
internal static class KeyboardPaste
{
    private const int DelayMs = 60;

    public static async Task IntoAsync(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero) return;

        NativeMethods.SetForegroundWindow(targetWindow);
        await Task.Delay(DelayMs);

        NativeMethods.keybd_event(NativeMethods.VirtualKeyControl, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyV, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyV, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyControl, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
    }
}
