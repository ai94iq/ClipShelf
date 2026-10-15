using System.Runtime.Versioning;
namespace Copyloft.Desktop.Platform.Windows;

// Sends Ctrl+V to the window the user was in before the flyout opened.
[SupportedOSPlatform("windows")]
internal sealed class KeyboardPaste : IPasteBack
{
    private const int DelayMs = 60;

    public async Task IntoAsync(IntPtr targetWindow)
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
