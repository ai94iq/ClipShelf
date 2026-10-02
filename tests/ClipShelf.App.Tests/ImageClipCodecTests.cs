using ClipShelf.App.Platform;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ClipShelf.App.Tests;

public sealed class ImageClipCodecTests
{
    [Fact]
    public async Task A_dib_becomes_a_png()
    {
        var png = await ImageClipCodec.ToPngAsync(SampleDib(2, 2));

        Assert.NotNull(png);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png!.Take(4).ToArray());
    }

    [Fact]
    public async Task The_thumbnail_fits_the_row()
    {
        var png = await ImageClipCodec.ToPngAsync(SampleDib(400, 200));
        Assert.NotNull(png);

        var thumbnail = await ImageClipCodec.ThumbnailAsync(png!);
        Assert.NotNull(thumbnail);

        using var stream = new MemoryStream(thumbnail!);
        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());

        Assert.True(decoder.PixelWidth <= 240);
        Assert.True(decoder.PixelHeight <= 160);
    }

    // A minimal 24-bpp DIB the decoder accepts.
    private static byte[] SampleDib(int width, int height)
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
