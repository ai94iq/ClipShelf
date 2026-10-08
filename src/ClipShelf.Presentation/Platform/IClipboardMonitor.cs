namespace ClipShelf.Presentation.Platform;

// The clipboard surface a head provides: change notifications plus reading and writing text and
// images. IClipboardWriter is the subset the view models use.
public interface IClipboardMonitor : IClipboardWriter
{
    event EventHandler? Changed;

    string? ReadText();

    // Platform-native image payload (a DIB block on Windows); the head decodes it.
    byte[]? ReadImage();
}
