using SkiaSharp;

namespace ClipShelf.Desktop.Imaging;

// Converts between the clipboard's DIB blocks and PNG bytes with Skia. Replaces the WinRT codec,
// which cannot exist off the windows TFM. Every entry point returns null for malformed input;
// nothing here may throw on clipboard data.
public static class SkiaImageCodec
{
    private const int HeaderSize = 40;   // BITMAPINFOHEADER

    public static byte[]? ToPng(byte[] dib)
    {
        using var bitmap = DecodeDib(dib);
        if (bitmap is null) return null;

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray();
    }

    public static byte[]? Thumbnail(byte[] png, int maxEdge)
    {
        if (maxEdge < 1) return null;

        using var bitmap = TryDecode(png);
        if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0) return null;

        var scale = Math.Min(1.0, (double)maxEdge / Math.Max(bitmap.Width, bitmap.Height));
        var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
        var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

        using var scaled = bitmap.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default);
        if (scaled is null) return null;

        using var image = SKImage.FromBitmap(scaled);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray();
    }

    public static byte[]? PngToDib(byte[] png)
    {
        using var bitmap = TryDecode(png);
        if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0) return null;

        var width = bitmap.Width;
        var height = bitmap.Height;
        var stride = width * 4;
        var pixels = stride * height;
        var dib = new byte[HeaderSize + pixels];
        var span = dib.AsSpan();

        BitConverter.TryWriteBytes(span[0..], HeaderSize);    // biSize
        BitConverter.TryWriteBytes(span[4..], width);         // biWidth
        BitConverter.TryWriteBytes(span[8..], height);        // biHeight (bottom-up)
        BitConverter.TryWriteBytes(span[12..], (short)1);     // biPlanes
        BitConverter.TryWriteBytes(span[14..], (short)32);    // biBitCount
        BitConverter.TryWriteBytes(span[20..], pixels);       // biSizeImage

        var source = bitmap.GetPixelSpan();
        for (var y = 0; y < height; y++)
        {
            source.Slice((height - 1 - y) * stride, stride)
                .CopyTo(span.Slice(HeaderSize + y * stride, stride));
        }

        return dib;
    }

    // SkiaSharp's decoder can throw instead of returning null for malformed input; this boundary
    // turns any decode failure into null, because clipboard data must never crash the app.
    private static SKBitmap? TryDecode(byte[] encoded)
    {
        if (encoded.Length < 8) return null;

        try
        {
            return SKBitmap.Decode(encoded);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Reads a CF_DIB block with the same bounds discipline as the clipboard reader: validate the
    // header against the buffer and never trust a size blindly.
    private static SKBitmap? DecodeDib(byte[] dib)
    {
        if (dib.Length < HeaderSize) return null;

        var headerBytes = BitConverter.ToUInt32(dib, 0);
        var width = BitConverter.ToInt32(dib, 4);
        var height = BitConverter.ToInt32(dib, 8);
        var bitCount = BitConverter.ToUInt16(dib, 14);

        if (headerBytes != HeaderSize || width <= 0 || height == 0) return null;
        if (bitCount is not (24 or 32)) return null;

        var topDown = height < 0;
        var absHeight = Math.Abs(height);

        var stride = ((long)width * bitCount + 31) / 32 * 4;
        var required = HeaderSize + stride * absHeight;
        if (required > dib.Length) return null;

        var info = new SKImageInfo(width, absHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        var destination = bitmap.GetPixelSpan();

        for (var y = 0; y < absHeight; y++)
        {
            var sourceRow = (topDown ? y : absHeight - 1 - y) * stride;
            var row = dib.AsSpan((int)(HeaderSize + sourceRow), (int)stride);
            var target = destination.Slice(y * width * 4);

            if (bitCount == 32)
            {
                row[..(width * 4)].CopyTo(target);
            }
            else
            {
                for (var x = 0; x < width; x++)
                {
                    target[x * 4] = row[x * 3];
                    target[x * 4 + 1] = row[x * 3 + 1];
                    target[x * 4 + 2] = row[x * 3 + 2];
                    target[x * 4 + 3] = 0xFF;
                }
            }
        }

        return bitmap;
    }
}
