using ClipShelf.Desktop.Platform;
using SkiaSharp;

namespace ClipShelf.Desktop.Tests;

public sealed class ImageClipCodecTests
{
    [Fact]
    public void A_dib_becomes_a_png() =>
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, ImageClipCodec.ToPng(SampleDib(2, 2))!.Take(4));

    [Fact]
    public void The_thumbnail_fits_the_row()
    {
        var png = ImageClipCodec.ToPng(SampleDib(400, 200))!;

        using var thumbnail = SKBitmap.Decode(ImageClipCodec.Thumbnail(png)!);

        Assert.True(thumbnail.Width <= 240 && thumbnail.Height <= 160, $"{thumbnail.Width}x{thumbnail.Height}");
    }

    [Fact]
    public void A_small_image_is_not_upscaled_for_the_thumbnail()
    {
        using var thumbnail = SKBitmap.Decode(ImageClipCodec.Thumbnail(ImageClipCodec.ToPng(SampleDib(10, 6))!)!);

        Assert.Equal((10, 6), (thumbnail.Width, thumbnail.Height));
    }

    [Fact]
    public void A_png_round_trips_through_a_dib_with_its_pixels()
    {
        using var source = new SKBitmap(3, 2);
        source.Erase(SKColors.Red);
        source.SetPixel(0, 0, SKColors.Blue);           // top-left, catches a flipped row order
        var png = source.Encode(SKEncodedImageFormat.Png, 100).ToArray();

        using var back = SKBitmap.Decode(ImageClipCodec.ToPng(ImageClipCodec.ToDib(png)!)!);

        Assert.Equal((3, 2), (back.Width, back.Height));
        Assert.Equal(SKColors.Blue, back.GetPixel(0, 0));
        Assert.Equal(SKColors.Red, back.GetPixel(2, 1));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 40, 0, 0, 0, 1 })]          // header says 40 bytes, the block is cut short
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 0, 0, 0, 0 })]   // absurd header size
    public void A_corrupt_dib_is_skipped_not_thrown(byte[] dib) => Assert.Null(ImageClipCodec.ToPng(dib));

    [Fact]
    public void Garbage_is_not_a_png() => Assert.Null(ImageClipCodec.ToDib([1, 2, 3]));

    // A minimal 24-bpp DIB the decoder accepts.
    internal static byte[] SampleDib(int width, int height)
    {
        var rowSize = ((width * 3 + 3) / 4) * 4;
        var pixels = rowSize * height;
        var dib = new byte[40 + pixels];
        BitConverter.TryWriteBytes(dib.AsSpan(0), 40);
        BitConverter.TryWriteBytes(dib.AsSpan(4), width);
        BitConverter.TryWriteBytes(dib.AsSpan(8), height);
        BitConverter.TryWriteBytes(dib.AsSpan(12), (short)1);
        BitConverter.TryWriteBytes(dib.AsSpan(14), (short)24);
        BitConverter.TryWriteBytes(dib.AsSpan(20), pixels);
        return dib;
    }
}
