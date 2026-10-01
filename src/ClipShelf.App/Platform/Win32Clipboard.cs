using System.Runtime.InteropServices;

namespace ClipShelf.App.Platform;

// Reads and writes plain-text clipboard content through the Win32 API. Another app can hold the
// clipboard open briefly, so opening is retried a few times before giving up. Reads run off the
// UI thread because the retries can sleep.
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

    public static bool TryWriteText(string text)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    if (!NativeMethods.EmptyClipboard()) return false;

                    // Room for the text plus the terminating null character.
                    var bytes = (nuint)((text.Length + 1) * sizeof(char));
                    var handle = NativeMethods.GlobalAlloc(
                        NativeMethods.GlobalMemoryMoveable | NativeMethods.GlobalMemoryZeroInit, bytes);
                    if (handle == IntPtr.Zero) return false;

                    var pointer = NativeMethods.GlobalLock(handle);
                    if (pointer == IntPtr.Zero)
                    {
                        NativeMethods.GlobalFree(handle);
                        return false;
                    }
                    try
                    {
                        Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(handle);
                    }

                    // Ownership passes to the clipboard on success; only free it if that failed.
                    if (NativeMethods.SetClipboardData(NativeMethods.ClipboardFormatUnicodeText, handle) == IntPtr.Zero)
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
