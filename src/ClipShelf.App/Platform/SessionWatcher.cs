using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClipShelf.App.Platform;

// A hidden top-level window that only listens for the session-end notification, so the history
// can be cleared on sign-out or shutdown before Windows tears the process down.
internal sealed class SessionWatcher
{
    private const string ClassName = "ClipShelf.SessionWindow";

    private delegate IntPtr WindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    // Held statically so the window class never points at a collected delegate.
    private static readonly WindowProc Proc = HandleMessage;
    private static SessionWatcher? _current;
    private static bool _classRegistered;

    public SessionWatcher()
    {
        _current = this;
        RegisterClassOnce();

        var handle = NativeMethods.CreateWindowEx(
            0, ClassName, ClassName, 0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        if (handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Creating the session window failed.");
    }

    // Raised when Windows is ending the session (sign-out or shutdown).
    public event EventHandler? Ending;

    private static void RegisterClassOnce()
    {
        if (_classRegistered) return;

        var windowClass = new NativeMethods.WndClassEx
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.WndClassEx>(),
            LpfnWndProc = Marshal.GetFunctionPointerForDelegate(Proc),
            LpszClassName = ClassName,
        };

        var atom = NativeMethods.RegisterClassEx(ref windowClass);
        if (atom == 0 && Marshal.GetLastWin32Error() != NativeMethods.ErrorClassAlreadyExists)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Registering the session window class failed.");

        _classRegistered = true;
    }

    private static IntPtr HandleMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == NativeMethods.MessageEndSession && wParam != IntPtr.Zero)
            _current?.Ending?.Invoke(_current, EventArgs.Empty);

        return NativeMethods.DefWindowProc(window, message, wParam, lParam);
    }
}
