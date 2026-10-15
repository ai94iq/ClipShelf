using System.Diagnostics;

namespace ClipShelf.Desktop.Platform;

// Whether a tray icon can show. Linux trays are StatusNotifierItems, shown only while a
// StatusNotifierWatcher owns its D-Bus name (KDE, or GNOME with the AppIndicator extension).
public static class TrayHost
{
    public static bool IsAvailable()
    {
        if (!OperatingSystem.IsLinux()) return true;
        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "dbus-send",
                ["--session", "--print-reply", "--dest=org.freedesktop.DBus", "/org/freedesktop/DBus",
                 "org.freedesktop.DBus.NameHasOwner", "string:org.kde.StatusNotifierWatcher"])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (process is null) return false;
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(2000)) return false;
            return HasOwner(output.Result);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false; // no dbus-send means no session bus to host a tray either
        }
    }

    internal static bool HasOwner(string dbusSendOutput) => dbusSendOutput.Contains("boolean true", StringComparison.Ordinal);
}
