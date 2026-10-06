using ClipShelf.App.Hosting;

namespace ClipShelf.App.Tests;

public sealed class AppFontsTests
{
    [Fact]
    public void Rtl_uses_the_bundled_arabic_font()
    {
        Assert.Contains("ms-appx", AppFonts.Family(rtl: true));
        Assert.Contains("Noto Sans Arabic", AppFonts.Family(rtl: true));
    }

    [Fact]
    public void Non_rtl_keeps_the_system_font()
    {
        Assert.Contains("Segoe UI", AppFonts.Family(rtl: false));
        Assert.DoesNotContain("Noto", AppFonts.Family(rtl: false));
    }
}
