using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ClipShelf.Desktop.Platform.Linux.X11Native;

#pragma warning disable SYSLIB1054 // DllImport is intentional: the app ships self-contained, not Native AOT.

namespace ClipShelf.Desktop.Platform.Linux;

// Our own X connection, used only from its own thread (so Xlib needs no XInitThreads): the
// thread waits on the X socket and a wake pipe, handles events, and runs the requests other
// threads queue through Run. Events are raised on that thread.
[SupportedOSPlatform("linux")]
public sealed class X11Connection
{
    private readonly ConcurrentQueue<Action> _work = new();
    private readonly int[] _wake = new int[2];
    private readonly int _fixesEventBase;
    private static int _trappedError;

    public X11Connection()
    {
        Display = XOpenDisplay(IntPtr.Zero);
        if (Display == IntPtr.Zero)
            throw new InvalidOperationException("No X display; ClipShelf on Linux needs X11 or XWayland.");
        Root = XDefaultRootWindow(Display);
        if (XFixesQueryExtension(Display, out _fixesEventBase, out _) == 0)
            throw new InvalidOperationException("The X server has no XFixes extension.");
        XFixesSelectSelectionInput(Display, Root, Atom("CLIPBOARD"), XFixesSetSelectionOwnerNotifyMask);
        XkbSetDetectableAutoRepeat(Display, 1, out _);
        XFlush(Display);

        if (pipe(_wake) != 0)
            throw new InvalidOperationException("Creating the X wake pipe failed.");
        new Thread(Loop) { IsBackground = true, Name = "X11 events" }.Start();
    }

    internal IntPtr Display { get; }
    internal IntPtr Root { get; }

    // Some app took ownership of CLIPBOARD (a copy).
    public event Action? SelectionChanged;

    // A grabbed key went down (true) or up (false): keycode, modifier state. With detectable
    // auto-repeat a held key repeats presses without releases in between.
    public event Action<uint, uint, bool>? KeyChanged;

    internal IntPtr Atom(string name) => XInternAtom(Display, name, 0);

    // Runs X requests on the connection's thread and returns their result.
    internal Task<T> Run<T>(Func<T> request)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _work.Enqueue(() =>
        {
            try { done.SetResult(request()); }
            catch (Exception ex) { done.SetException(ex); }
        });
        write(_wake[1], [1], 1);
        return done.Task;
    }

    // On the connection's thread: runs requests and reports whether the server refused any with
    // BadAccess (a key another client already grabbed). The handler is swapped in only around them.
    internal bool Trap(Action requests)
    {
        _trappedError = 0;
        var previous = XSetErrorHandler(Marshal.GetFunctionPointerForDelegate(TrapHandler));
        try
        {
            requests();
            XSync(Display, 0);
        }
        finally
        {
            XSetErrorHandler(previous);
        }
        return _trappedError != BadAccess;
    }

    private delegate int ErrorHandler(IntPtr display, ref XErrorEvent error);

    // Held statically so Xlib never calls a collected delegate.
    private static readonly ErrorHandler TrapHandler = (IntPtr _, ref XErrorEvent error) =>
    {
        _trappedError = error.ErrorCode;
        return 0;
    };

    private void Loop()
    {
        var fds = new PollFd[]
        {
            new() { Fd = XConnectionNumber(Display), Events = PollIn },
            new() { Fd = _wake[0], Events = PollIn },
        };
        var drain = new byte[64];
        while (true)
        {
            while (XPending(Display) > 0)
            {
                XNextEvent(Display, out var e);
                if (e.Type == _fixesEventBase) SelectionChanged?.Invoke();
                else if (e.Type is KeyPress or KeyRelease) KeyChanged?.Invoke(e.KeyCode, e.KeyState, e.Type == KeyPress);
            }

            while (_work.TryDequeue(out var work)) work();
            // A request's XSync can pull events into Xlib's queue, where poll would never see them.
            if (XPending(Display) > 0) continue;

            if (poll(fds, 2, -1) > 0 && (fds[1].Revents & PollIn) != 0)
                read(_wake[0], drain, drain.Length);
        }
    }

    private const short PollIn = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    [DllImport("libX11.so.6")] private static extern int XPending(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XConnectionNumber(IntPtr display);
    [DllImport("libc", SetLastError = true)] private static extern int pipe(int[] fds);
    [DllImport("libc")] private static extern int poll([In, Out] PollFd[] fds, nuint count, int timeout);
    [DllImport("libc")] private static extern nint read(int fd, byte[] buffer, nint count);
    [DllImport("libc")] private static extern nint write(int fd, byte[] buffer, nint count);
}
