using ClipShelf.Desktop.Platform.Linux;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the X11 target negotiation: pure logic, no display.
public sealed class X11SelectionTests
{
    [Fact]
    public void PickReadTarget_PrefersImageOverText() =>
        Assert.Equal("image/png", X11Selection.PickReadTarget(["UTF8_STRING", "image/png", "STRING"]));

    [Fact]
    public void PickReadTarget_PrefersUtf8OverLegacy() =>
        Assert.Equal("UTF8_STRING", X11Selection.PickReadTarget(["STRING", "TEXT", "UTF8_STRING"]));

    [Fact]
    public void PickReadTarget_FallsBackToLegacyString() =>
        Assert.Equal("STRING", X11Selection.PickReadTarget(["STRING"]));

    [Fact]
    public void PickReadTarget_ReturnsNullWhenNothingUsable() =>
        Assert.Null(X11Selection.PickReadTarget(["TIMESTAMP", "SAVE_TARGETS"]));

    [Fact]
    public void ServeTargets_ListsImageFormatsForImages() =>
        Assert.Equal(["TARGETS", "image/png", "UTF8_STRING", "STRING", "TEXT"], X11Selection.ServeTargets(true));

    [Fact]
    public void ServeTargets_OmitsImageFormatsForText() =>
        Assert.Equal(["TARGETS", "UTF8_STRING", "STRING", "TEXT"], X11Selection.ServeTargets(false));
}
