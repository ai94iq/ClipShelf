namespace ClipShelf.App.Services;

// Writing clipboard content back. An interface so view models can be tested without Win32.
public interface IClipboardWriter
{
    void WriteText(string text);

    // Puts a PNG back on the clipboard so image clips paste into apps that take bitmaps.
    Task WriteImageAsync(byte[] png);
}
