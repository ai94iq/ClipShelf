using ClipShelf.Desktop.Platform.Linux;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the Linux autostart entry: exact file content and the
// create/remove round trip under an isolated config home. File IO only, so it
// runs on any OS.
public sealed class LinuxStartupTests : IDisposable
{
    private readonly string _configHome = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string? _previous;

    public LinuxStartupTests()
    {
        _previous = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _configHome);
    }

    [Fact]
    public void Apply_CreatesEntryWithExpectedContent()
    {
#pragma warning disable CA1416 // File IO only; no native calls on this path.
        var startup = new LinuxStartup(NullLogger<LinuxStartup>.Instance);
        startup.Apply(true);
#pragma warning restore CA1416
        var file = Path.Combine(_configHome, "autostart", "clipshelf.desktop");
        Assert.True(File.Exists(file));
        var content = File.ReadAllText(file);
        Assert.Contains("[Desktop Entry]", content);
        Assert.Contains("Name=ClipShelf", content);
        Assert.Contains("Terminal=false", content);
        Assert.Contains("Exec=", content);
    }

    [Fact]
    public void Apply_RemovesEntry()
    {
#pragma warning disable CA1416 // File IO only; no native calls on this path.
        var startup = new LinuxStartup(NullLogger<LinuxStartup>.Instance);
        startup.Apply(true);
        startup.Apply(false);
#pragma warning restore CA1416
        Assert.False(File.Exists(Path.Combine(_configHome, "autostart", "clipshelf.desktop")));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _previous);
        if (Directory.Exists(_configHome)) Directory.Delete(_configHome, true);
    }
}
