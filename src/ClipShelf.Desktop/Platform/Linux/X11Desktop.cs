using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using static ClipShelf.Desktop.Platform.Linux.X11Native;

namespace ClipShelf.Desktop.Platform.Linux;

// The EWMH side of X11: the active window and its process (_NET_ACTIVE_WINDOW, _NET_WM_PID),
// activating a window through the window manager, the pointer, and Ctrl+V through XTest.
[SupportedOSPlatform("linux")]
public sealed class X11Desktop(X11Connection x, bool? wayland = null) : IForegroundApp, IFlyoutPlatform, IPasteBack
{
    private const int DelayMs = 60;
    private readonly bool _wayland = wayland ?? DesktopSession.IsWayland;
    private const uint KeysymControl = 0xFFE3, KeysymV = 0x76;

    // Under Wayland the active X window is at best another X client (often ClipShelf's own
    // History window), never the Wayland app the copy came from, so the name stays blank.
    public string? Name()
    {
        if (_wayland) return null;
        var pid = Wait(() => Cardinal(ActiveWindow(), "_NET_WM_PID"));
        if (pid is not > 0) return null;
        try
        {
            return Process.GetProcessById((int)pid).ProcessName;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public IntPtr ForegroundWindow() => Wait(ActiveWindow);

    public PixelPoint Cursor() => Wait(() =>
        XQueryPointer(x.Display, x.Root, out _, out _, out var rootX, out var rootY, out _, out _, out _) != 0
            ? new PixelPoint(rootX, rootY)
            : default);

    public void ForceForeground(IntPtr window)
    {
        if (window != IntPtr.Zero) Wait(() => Activate(window));
    }

    public void RestoreForeground(IntPtr own, IntPtr previous)
    {
        if (previous != IntPtr.Zero) Wait(() => ActiveWindow() == own && Activate(previous));
    }

    public bool CanPaste => !_wayland;

    public async Task IntoAsync(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero) return;
        await x.Run(() => Activate(targetWindow));
        await Task.Delay(DelayMs);
        await x.Run(() =>
        {
            var control = XKeysymToKeycode(x.Display, (IntPtr)KeysymControl);
            var v = XKeysymToKeycode(x.Display, (IntPtr)KeysymV);
            XTestFakeKeyEvent(x.Display, control, 1, 0);
            XTestFakeKeyEvent(x.Display, v, 1, 0);
            XTestFakeKeyEvent(x.Display, v, 0, 0);
            XTestFakeKeyEvent(x.Display, control, 0, 0);
            return true;
        });
    }

    private T Wait<T>(Func<T> request) => x.Run(request).GetAwaiter().GetResult();

    private IntPtr ActiveWindow() => (IntPtr)(Cardinal(x.Root, "_NET_ACTIVE_WINDOW") ?? 0);

    // Source indication 2 (a pager) asks the window manager to honor the request even though
    // the user did not just click that window.
    private bool Activate(IntPtr window)
    {
        var message = new XClientMessageEvent
        {
            Type = ClientMessage,
            Window = window,
            MessageType = x.Atom("_NET_ACTIVE_WINDOW"),
            Format = 32,
            Data0 = 2,
        };
        XSendEvent(x.Display, x.Root, 0, (nint)(SubstructureRedirectMask | SubstructureNotifyMask), ref message);
        return true;
    }

    // The first 32-bit item of a window property (format-32 items arrive as C longs).
    private long? Cardinal(IntPtr window, string property)
    {
        if (window == IntPtr.Zero) return null;
        if (XGetWindowProperty(x.Display, window, x.Atom(property), 0, 1, 0, IntPtr.Zero,
                out _, out var format, out var items, out _, out var data) != 0 || data == IntPtr.Zero)
            return null;
        try
        {
            return format == 32 && items > 0 ? Marshal.ReadInt64(data) : null;
        }
        finally
        {
            XFree(data);
        }
    }
}
