using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Foreground app until the X11 _NET_ACTIVE_WINDOW query lands (Phase 3 task):
// source labels fall back to the app-name default.
[SupportedOSPlatform("linux")]
public sealed class LinuxForegroundApp : IForegroundApp
{
    public string? Name() => null;
}
