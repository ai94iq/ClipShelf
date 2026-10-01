using ClipShelf.App.Platform;
using ClipShelf.Core.Services;

namespace ClipShelf.App.Services;

// Turns clipboard changes into history entries and writes items back. Writes made by this app are
// counted so the resulting clipboard-changed message does not get captured as a new item.
public sealed class ClipboardService : IDisposable
{
    private readonly ClipCaptureService _capture;
    private readonly SettingsService _settings;
    private readonly ILogger<ClipboardService> _log;
    private readonly ClipboardWatcher _watcher = new();
    private int _selfWrites;

    public ClipboardService(ClipCaptureService capture, SettingsService settings, ILogger<ClipboardService> log)
    {
        _capture = capture;
        _settings = settings;
        _log = log;
        _watcher.Changed += OnClipboardChanged;
    }

    public void WriteText(string text)
    {
        _selfWrites++;
        if (Win32Clipboard.TryWriteText(text)) return;

        _selfWrites--;
        _log.LogWarning("Writing text to the clipboard failed");
    }

    public void Dispose() => _watcher.Dispose();

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        if (_selfWrites > 0)
        {
            _selfWrites--;
            return;
        }

        var text = Win32Clipboard.TryReadText();
        if (text is null) return;

        _ = CaptureAsync(text);
    }

    private async Task CaptureAsync(string text)
    {
        try
        {
            await _capture.CaptureAsync(
                text,
                ForegroundApp.Name(),
                DateTimeOffset.UtcNow,
                _settings.Current.MaxItems,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Capturing a clipboard change failed");
        }
    }
}
