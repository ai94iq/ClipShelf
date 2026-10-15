using System.Runtime.Versioning;
using Avalonia;

namespace Copyloft.Desktop.Platform.Windows;

[SupportedOSPlatform("windows")]
internal sealed class WindowsFlyoutPlatform : IFlyoutPlatform
{
    public IntPtr ForegroundWindow() => NativeMethods.GetForegroundWindow();

    public PixelPoint Cursor() =>
        NativeMethods.GetCursorPos(out var point) ? new PixelPoint(point.X, point.Y) : default;

    // Windows refuses SetForegroundWindow while another app owns the foreground (a hotkey or a
    // tray click are not enough), so tap ALT first, the standard way to lift that restriction.
    public void ForceForeground(IntPtr window)
    {
        if (window == IntPtr.Zero || NativeMethods.GetForegroundWindow() == window) return;

        NativeMethods.keybd_event(NativeMethods.VirtualKeyMenu, 0, 0, UIntPtr.Zero);
        NativeMethods.SetForegroundWindow(window);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyMenu, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
    }

    // Give the foreground back to the app the user came from, so keystrokes do not land here.
    public void RestoreForeground(IntPtr own, IntPtr previous)
    {
        if (previous != IntPtr.Zero && NativeMethods.GetForegroundWindow() == own)
            NativeMethods.SetForegroundWindow(previous);
    }
}
