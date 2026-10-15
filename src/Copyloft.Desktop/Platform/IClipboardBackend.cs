namespace Copyloft.Desktop.Platform;

// One OS's clipboard: change notifications (raised on the UI thread) and reading and writing
// text and PNG images. Reads return null when that kind of content is not there.
public interface IClipboardBackend : IDisposable
{
    event EventHandler? Changed;
    Task<string?> ReadTextAsync();
    Task<byte[]?> ReadImagePngAsync();
    Task<bool> WriteTextAsync(string text);
    Task<bool> WriteImageAsync(byte[] png);
}
