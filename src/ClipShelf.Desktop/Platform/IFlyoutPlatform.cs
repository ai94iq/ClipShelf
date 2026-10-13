using Avalonia;

namespace ClipShelf.Desktop.Platform;

// What the flyout needs from the OS: who had the foreground, where the pointer is, and taking
// and handing back the foreground.
public interface IFlyoutPlatform
{
    IntPtr ForegroundWindow();

    PixelPoint Cursor();

    void ForceForeground(IntPtr window);

    void RestoreForeground(IntPtr own, IntPtr previous);
}
