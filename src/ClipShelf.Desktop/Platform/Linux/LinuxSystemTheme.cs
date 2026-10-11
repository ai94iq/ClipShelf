using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// System theme until the portal/desktop-setting query lands (Phase 3 task):
// assume a light taskbar and never change.
[SupportedOSPlatform("linux")]
public sealed class LinuxSystemTheme : ISystemTheme
{
    public bool TaskbarIsLight => true;

#pragma warning disable CS0067 // Raised by the portal query in a later Phase 3 task.
    public event EventHandler? Changed;
#pragma warning restore CS0067
}
