namespace ClipShelf.Desktop.Tests;

// Builds the smallest valid CF_DIB blocks for the codec tests: a 40-byte BITMAPINFOHEADER and a
// 32bpp BGRA pixel block, bottom-up like the clipboard delivers.
internal static class DibFactory
{
    public static byte[] Create32bpp(int width, int height)
    {
        const int headerSize = 40;
        var pixels = width * height * 4;
        var dib = new byte[headerSize + pixels];
        var span = dib.AsSpan();

        BitConverter.TryWriteBytes(span[0..], headerSize);   // biSize
        BitConverter.TryWriteBytes(span[4..], width);        // biWidth
        BitConverter.TryWriteBytes(span[8..], height);       // biHeight (bottom-up)
        BitConverter.TryWriteBytes(span[12..], (short)1);    // biPlanes
        BitConverter.TryWriteBytes(span[14..], (short)32);   // biBitCount
        BitConverter.TryWriteBytes(span[20..], pixels);      // biSizeImage

        for (var i = 0; i < pixels; i += 4)
        {
            span[headerSize + i] = 0x40;      // B
            span[headerSize + i + 1] = 0x80;  // G
            span[headerSize + i + 2] = 0xC0;  // R
            span[headerSize + i + 3] = 0xFF;  // A
        }

        return dib;
    }
}
