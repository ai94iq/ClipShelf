using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Session end until the SIGTERM hook lands (Phase 3 task): never fires, so
// clear-on-sign-out is best effort on Linux for now.
[SupportedOSPlatform("linux")]
public sealed class LinuxSessionEnd : ISessionEnd
{
#pragma warning disable CS0067 // Raised by the SIGTERM hook in a later Phase 3 task.
    public event EventHandler? Ending;
#pragma warning restore CS0067
}
