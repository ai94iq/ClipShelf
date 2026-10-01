using System.Diagnostics;

namespace ClipShelf.App.Platform;

// The process name of the window that was in front when the copy happened.
internal static class ForegroundApp
{
    public static string? Name()
    {
        var window = NativeMethods.GetForegroundWindow();
        if (window == IntPtr.Zero) return null;

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0) return null;

        try
        {
            return Process.GetProcessById((int)processId).ProcessName;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
