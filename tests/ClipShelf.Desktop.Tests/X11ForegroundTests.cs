using ClipShelf.Desktop.Platform.Linux;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the pid parsing: pure logic, no display.
public sealed class X11ForegroundTests
{
// ParsePid is pure; the linux tag lives on the service class around it.
#pragma warning disable CA1416
    [Fact]
    public void ParsePid_ReadsFirstCardinal()
    {
        Assert.Equal(1234, X11ForegroundApp.ParsePid([0xD2, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));
    }

    [Fact]
    public void ParsePid_RejectsShortOrMissingInput()
    {
        Assert.Null(X11ForegroundApp.ParsePid(null));
        Assert.Null(X11ForegroundApp.ParsePid([]));
        Assert.Null(X11ForegroundApp.ParsePid([0x01, 0x02]));
    }
#pragma warning restore CA1416
}
