using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace ClipShelf.Desktop.Platform.Windows;

// Reads and writes plain-text clipboard content through the Win32 API. Another app can hold the
// clipboard open briefly, so opening is retried a few times before giving up. Reads run off the
// UI thread because the retries can sleep.
[SupportedOSPlatform("windows")]
internal static class Win32Clipboard
{
    private const int Retries = 5;
    private const int RetryDelayMs = 10;

    public static string? TryReadText()
    {
        for (var attempt = 0; ; attempt++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    var handle = NativeMethods.GetClipboardData(NativeMethods.ClipboardFormatUnicodeText);
                    if (handle == IntPtr.Zero) return null;

                    var pointer = NativeMethods.GlobalLock(handle);
                    if (pointer == IntPtr.Zero) return null;
                    try
                    {
                        return Marshal.PtrToStringUni(pointer);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(handle);
                    }
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            if (attempt >= Retries) return null;
            Thread.Sleep(RetryDelayMs);
        }
    }

    // True when the clipboard holds an image (CF_DIB, which Windows also synthesizes from CF_DIBV5).
    public static bool HasImage()
    {
        for (var attempt = 0; ; attempt++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    return NativeMethods.IsClipboardFormatAvailable(NativeMethods.ClipboardFormatDib);
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            if (attempt >= Retries) return false;
            Thread.Sleep(RetryDelayMs);
        }
    }

    // The raw CF_DIB bytes; the codec turns them into a PNG.
    public static byte[]? TryReadImage()
    {
        for (var attempt = 0; ; attempt++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    var handle = NativeMethods.GetClipboardData(NativeMethods.ClipboardFormatDib);
                    if (handle == IntPtr.Zero) return null;

                    var allocation = (ulong)NativeMethods.GlobalSize(handle);
                    var pointer = NativeMethods.GlobalLock(handle);
                    if (pointer == IntPtr.Zero) return null;
                    try
                    {
                        return ReadBounded(pointer, allocation);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(handle);
                    }
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            if (attempt >= Retries) return null;
            Thread.Sleep(RetryDelayMs);
        }
    }

    // Reads a CF_DIB block with the size cross-checked against the block's own header. GlobalSize
    // can overstate the allocation for handles rendered by another app, and trusting it walked off
    // the mapped memory in Marshal.Copy - an access violation .NET cannot catch, so the process
    // died. The copy length is the smaller of the header-derived size and the allocation.
    private static byte[]? ReadBounded(IntPtr pointer, ulong allocation)
    {
        const int HeaderSize = 40;                       // BITMAPINFOHEADER
        const long MaxImageBytes = 128L * 1024 * 1024;   // sanity bound; anything larger is not a screenshot

        if (allocation < HeaderSize || allocation > MaxImageBytes) return null;

        var header = new byte[HeaderSize];
        Marshal.Copy(pointer, header, 0, HeaderSize);

        var headerBytes = BitConverter.ToUInt32(header, 0);          // biSize
        var width = BitConverter.ToInt32(header, 4);
        var height = BitConverter.ToInt32(header, 8);
        var bitCount = BitConverter.ToUInt16(header, 14);            // biBitCount
        var palette = bitCount <= 8 ? (1L << bitCount) * 4 : 0;      // an upper bound covers biClrUsed
        var pixels = (((long)Math.Abs(width) * Math.Abs(height) * bitCount) + 7) / 8;
        var expected = (long)headerBytes + palette + pixels;
        if (expected <= HeaderSize) return null;

        var size = (int)Math.Min(expected, (long)allocation);
        var bytes = new byte[size];
        Marshal.Copy(pointer, bytes, 0, size);
        return bytes;
    }

    public static bool TryWriteText(string text)
    {
        // The text plus the terminating null character.
        var bytes = new byte[(text.Length + 1) * sizeof(char)];
        System.Text.Encoding.Unicode.GetBytes(text, 0, text.Length, bytes, 0);
        return TrySetData(NativeMethods.ClipboardFormatUnicodeText, bytes);
    }

    // A CF_DIB block (the codec's ToDib output); every Windows app reads images from it.
    public static bool TryWriteImage(byte[] dib) => TrySetData(NativeMethods.ClipboardFormatDib, dib);

    private static bool TrySetData(uint format, byte[] data)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    if (!NativeMethods.EmptyClipboard()) return false;

                    var handle = NativeMethods.GlobalAlloc(
                        NativeMethods.GlobalMemoryMoveable | NativeMethods.GlobalMemoryZeroInit, (nuint)data.Length);
                    if (handle == IntPtr.Zero) return false;

                    var pointer = NativeMethods.GlobalLock(handle);
                    if (pointer == IntPtr.Zero)
                    {
                        NativeMethods.GlobalFree(handle);
                        return false;
                    }
                    try
                    {
                        Marshal.Copy(data, 0, pointer, data.Length);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(handle);
                    }

                    // Ownership passes to the clipboard on success; only free it if that failed.
                    if (NativeMethods.SetClipboardData(format, handle) == IntPtr.Zero)
                    {
                        NativeMethods.GlobalFree(handle);
                        return false;
                    }

                    return true;
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            if (attempt >= Retries) return false;
            Thread.Sleep(RetryDelayMs);
        }
    }
}
