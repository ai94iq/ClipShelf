using System.Runtime.Versioning;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Platform.Windows;
using SkiaSharp;

namespace ClipShelf.Desktop.Tests;

// Uses the real clipboard of the machine running the tests.
public sealed class Win32ClipboardTests
{
    [Fact]
    [SupportedOSPlatform("windows")]
    public void An_image_written_as_a_dib_reads_back_with_its_size()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Win32 clipboard");

        using var source = new SKBitmap(4, 3);
        source.Erase(SKColors.Green);
        var png = source.Encode(SKEncodedImageFormat.Png, 100).ToArray();

        Assert.True(Win32Clipboard.TryWriteImage(ImageClipCodec.ToDib(png)!));

        using var back = SKBitmap.Decode(ImageClipCodec.ToPng(Win32Clipboard.TryReadImage()!)!);
        Assert.Equal((4, 3), (back.Width, back.Height));
    }
}
