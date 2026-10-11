using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Startup registration until the autostart .desktop entry lands (Phase 3
// task): flipping the switch is accepted and logged, but nothing is written.
[SupportedOSPlatform("linux")]
public sealed class LinuxStartup(ILogger<LinuxStartup> log) : IStartupRegistration
{
    public void Apply(bool enabled) =>
        log.LogWarning("Run at startup is not implemented on Linux yet (enabled: {Enabled})", enabled);
}
