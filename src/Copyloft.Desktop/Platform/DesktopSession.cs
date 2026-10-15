namespace Copyloft.Desktop.Platform;

// The kind of Linux session the app runs in. Under Wayland the app still runs through XWayland,
// but X input (key grabs, XTest) only reaches X windows, not the rest of the desktop.
public static class DesktopSession
{
    public static bool IsWayland { get; } = OperatingSystem.IsLinux() && (
        Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland"
        || Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is { Length: > 0 });
}
