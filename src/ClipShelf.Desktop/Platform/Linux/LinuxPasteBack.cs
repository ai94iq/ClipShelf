using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Paste-back until XTest lands (Phase 3 task). Unreachable for now: the flyout
// stays Windows-only until then.
[SupportedOSPlatform("linux")]
public sealed class LinuxPasteBack : IPasteBack
{
    public Task IntoAsync(IntPtr targetWindow) => Task.CompletedTask;
}
