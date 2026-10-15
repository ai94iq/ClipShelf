using System.Runtime.Versioning;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace ClipShelf.Desktop.Platform.Linux;

// XFixes tells us when CLIPBOARD changes owner; Avalonia's clipboard does the reading and writing
// (selection ownership and large transfers). Images travel as image/png, the X11 convention.
[SupportedOSPlatform("linux")]
public sealed class X11ClipboardBackend : IClipboardBackend
{
    private static readonly DataFormat<byte[]> Png = DataFormat.CreateBytesPlatformFormat("image/png");

    private readonly X11Connection _x;
    private readonly IUiDispatcher _ui;
    private readonly Func<IClipboard?> _clipboard;

    public X11ClipboardBackend(X11Connection x, IUiDispatcher ui, Func<IClipboard?> clipboard)
    {
        _x = x;
        _ui = ui;
        _clipboard = clipboard;
        _x.SelectionChanged += OnSelectionChanged;
    }

    public event EventHandler? Changed;

    public async Task<string?> ReadTextAsync() =>
        _clipboard() is { } clipboard ? await clipboard.TryGetTextAsync() : null;

    public async Task<byte[]?> ReadImagePngAsync() =>
        _clipboard() is { } clipboard ? await clipboard.TryGetValueAsync(Png) : null;

    public async Task<bool> WriteTextAsync(string text)
    {
        if (_clipboard() is not { } clipboard) return false;
        await clipboard.SetTextAsync(text);
        return true;
    }

    public async Task<bool> WriteImageAsync(byte[] png)
    {
        if (_clipboard() is not { } clipboard) return false;
        await clipboard.SetValueAsync(Png, png);
        return true;
    }

    public void Dispose() => _x.SelectionChanged -= OnSelectionChanged;

    private void OnSelectionChanged() => _ui.Post(() => Changed?.Invoke(this, EventArgs.Empty));
}
