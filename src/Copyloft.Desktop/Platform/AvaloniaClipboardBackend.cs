using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Copyloft.Desktop.Platform;

// Reading and writing through Avalonia's clipboard, which handles selection ownership and large
// transfers. The OS layer only reports changes through NotifyChanged: XFixes on X11, a
// pasteboard poll on macOS. Images travel as PNG under the platform's own name for it.
public sealed class AvaloniaClipboardBackend(Func<IClipboard?> clipboard, string pngFormat) : IClipboardBackend
{
    // image/png on X11, public.png on macOS.
    private readonly DataFormat<byte[]> _png = DataFormat.CreateBytesPlatformFormat(pngFormat);

    public event EventHandler? Changed;

    // On the UI thread.
    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public async Task<string?> ReadTextAsync() =>
        clipboard() is { } board ? await board.TryGetTextAsync() : null;

    public async Task<byte[]?> ReadImagePngAsync() =>
        clipboard() is { } board ? await board.TryGetValueAsync(_png) : null;

    public async Task<bool> WriteTextAsync(string text)
    {
        if (clipboard() is not { } board) return false;
        await board.SetTextAsync(text);
        return true;
    }

    public async Task<bool> WriteImageAsync(byte[] png)
    {
        if (clipboard() is not { } board) return false;
        await board.SetValueAsync(_png, png);
        return true;
    }

    public void Dispose() { }
}
