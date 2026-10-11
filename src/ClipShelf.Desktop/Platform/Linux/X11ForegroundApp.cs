using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// The process name of the window in front, via _NET_ACTIVE_WINDOW plus
// _NET_WM_PID plus /proc. Per-call display connection: queries are paced by
// copies, so there is no lifetime to manage. Anything missing means unknown.
[SupportedOSPlatform("linux")]
public sealed class X11ForegroundApp(ILogger<X11ForegroundApp> log) : IForegroundApp
{
    public string? Name()
    {
        try
        {
            var display = X11Native.XOpenDisplay(null);
            if (display == IntPtr.Zero) return null;
            try
            {
                var root = X11Native.XDefaultRootWindow(display);
                var window = ReadWindow(display, root);
                if (window == IntPtr.Zero) return null;
                var pid = ReadPid(display, window);
                if (pid is null) return null;
                var comm = Path.Combine("/proc", pid.Value.ToString(CultureInfo.InvariantCulture), "comm");
                return File.Exists(comm) ? File.ReadAllText(comm).Trim() : null;
            }
            finally
            {
                X11Native.XCloseDisplay(display);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is IOException || ex is UnauthorizedAccessException)
        {
            log.LogDebug(ex, "Reading the foreground app failed.");
            return null;
        }
    }

    // A CARDINAL property arrives as 32-bit units; the pid is the first one.
    public static int? ParsePid(byte[]? raw) =>
        raw is null || raw.Length < 4 ? null : (int?)BitConverter.ToInt32(raw, 0);

    private static IntPtr ReadWindow(IntPtr display, IntPtr root)
    {
        var active = X11Native.XInternAtom(display, "_NET_ACTIVE_WINDOW", false);
        var incr = X11Native.XInternAtom(display, X11Selection.Incr, false);
        var bytes = X11Native.ReadBytes(display, root, active, incr, remove: false);
        if (bytes is null || bytes.Length < IntPtr.Size) return IntPtr.Zero;
        return new IntPtr(BitConverter.ToInt64(bytes, 0));
    }

    private static int? ReadPid(IntPtr display, IntPtr window)
    {
        var pid = X11Native.XInternAtom(display, "_NET_WM_PID", false);
        var incr = X11Native.XInternAtom(display, X11Selection.Incr, false);
        return ParsePid(X11Native.ReadBytes(display, window, pid, incr, remove: false));
    }
}
