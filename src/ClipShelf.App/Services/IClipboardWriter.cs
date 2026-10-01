namespace ClipShelf.App.Services;

// Writing text back to the clipboard. An interface so view models can be tested without Win32.
public interface IClipboardWriter
{
    void WriteText(string text);
}
