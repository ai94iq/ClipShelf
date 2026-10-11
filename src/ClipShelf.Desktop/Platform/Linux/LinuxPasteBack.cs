using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Sends Ctrl+V to whichever window holds focus, mirroring the Windows paste.
// The caller hides our window first so focus is already back where the user
// was. Per-call display connection: pastes are user-paced, so no lifetime to
// manage. Without XTest the paste is skipped with a warning.
[SupportedOSPlatform("linux")]
public sealed class LinuxPasteBack(ILogger<LinuxPasteBack> log) : IPasteBack
{
    private const int DelayMs = 60;
    private const ulong ControlKeysym = 0xFFE3;
    private const ulong VKeysym = 0x76;

    public async Task IntoAsync(IntPtr targetWindow)
    {
        _ = targetWindow;   // XTest pastes into the focused window; no handle needed.
        await Task.Delay(DelayMs).ConfigureAwait(false);
        try
        {
            var display = X11Native.XOpenDisplay(null);
            if (display == IntPtr.Zero) return;
            try
            {
                if (Native.XTestQueryExtension(display, out _, out _, out _, out _) == 0)
                {
                    log.LogWarning("XTest is unavailable; the paste was skipped.");
                    return;
                }

                var control = X11Native.XKeysymToKeycode(display, ControlKeysym);
                var v = X11Native.XKeysymToKeycode(display, VKeysym);
                if (control == 0 || v == 0)
                {
                    log.LogWarning("No keycodes for Ctrl+V; the paste was skipped.");
                    return;
                }

                Native.XTestFakeKeyEvent(display, control, true, 0);
                Native.XTestFakeKeyEvent(display, v, true, 0);
                Native.XTestFakeKeyEvent(display, v, false, 0);
                Native.XTestFakeKeyEvent(display, control, false, 0);
                X11Native.XFlush(display);
            }
            finally
            {
                X11Native.XCloseDisplay(display);
            }
        }
        catch (DllNotFoundException ex)
        {
            log.LogWarning(ex, "X11 libraries are unavailable; the paste was skipped.");
        }
    }

    private static class Native
    {
        [DllImport("libXtst.so.6")]
        internal static extern int XTestQueryExtension(
            IntPtr display, out int eventBase, out int errorBase,
            out int majorVersion, out int minorVersion);

        [DllImport("libXtst.so.6")]
        internal static extern int XTestFakeKeyEvent(
            IntPtr display, uint keycode, bool isPress, ulong delay);
    }
}
