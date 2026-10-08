using ClipShelf.Desktop.Imaging;
using SkiaSharp;

namespace ClipShelf.Desktop.Tests;

public sealed class SkiaImageCodecTests
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];

    [Fact]
    public void ToPng_converts_a_valid_dib()
    {
        var dib = DibFactory.Create32bpp(4, 3);

        var png = SkiaImageCodec.ToPng(dib);

        Assert.NotNull(png);
        Assert.True(png!.AsSpan(0, 4).SequenceEqual(PngMagic));
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        Assert.Equal(4, bitmap.Width);
        Assert.Equal(3, bitmap.Height);
    }

    [Fact]
    public void ToPng_returns_null_for_truncated_input()
    {
        Assert.Null(SkiaImageCodec.ToPng(new byte[20]));
    }

    [Fact]
    public void ToPng_returns_null_for_a_lying_header()
    {
        var dib = DibFactory.Create32bpp(4, 3);
        BitConverter.TryWriteBytes(dib.AsSpan(4), 10000);   // claims 10000x10000
        BitConverter.TryWriteBytes(dib.AsSpan(8), 10000);

        Assert.Null(SkiaImageCodec.ToPng(dib));
    }

    [Fact]
    public void Thumbnail_caps_the_long_edge()
    {
        var source = MakePng(400, 200);

        var thumbnail = SkiaImageCodec.Thumbnail(source, 96);

        Assert.NotNull(thumbnail);
        using var bitmap = SKBitmap.Decode(thumbnail);
        Assert.NotNull(bitmap);
        Assert.Equal(96, bitmap.Width);
        Assert.Equal(48, bitmap.Height);
    }

    [Fact]
    public void PngToDib_round_trips_dimensions()
    {
        var png = MakePng(7, 5);

        var dib = SkiaImageCodec.PngToDib(png);

        Assert.NotNull(dib);
        Assert.Equal(40, BitConverter.ToInt32(dib, 0));                 // biSize
        Assert.Equal(7, BitConverter.ToInt32(dib, 4));                  // biWidth
        Assert.Equal(5, BitConverter.ToInt32(dib, 8));                  // biHeight
        Assert.Equal(32, (int)BitConverter.ToInt16(dib, 14));           // biBitCount

        var roundTrip = SkiaImageCodec.ToPng(dib);
        Assert.NotNull(roundTrip);
        using var bitmap = SKBitmap.Decode(roundTrip);
        Assert.NotNull(bitmap);
        Assert.Equal(7, bitmap.Width);
        Assert.Equal(5, bitmap.Height);
    }

    [Fact]
    public void PngToDib_returns_null_for_garbage()
    {
        Assert.Null(SkiaImageCodec.PngToDib([0x01, 0x02, 0x03]));
    }

    private static byte[] MakePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
