using ClipShelf.App.Platform;
using ClipShelf.Core.Services;

namespace ClipShelf.App.Services;

// Turns clipboard changes into history entries and writes items back. Writes made by this app are
// counted so the resulting clipboard-changed message does not get captured as a new item.
public sealed class ClipboardService : IClipboardWriter, IDisposable
{
    private readonly ClipCaptureService _capture;
    private readonly IClipRepository _repository;
    private readonly SettingsService _settings;
    private readonly ILogger<ClipboardService> _log;
    private readonly ClipboardWatcher _watcher = new();
    private int _selfWrites;

    public ClipboardService(
        ClipCaptureService capture,
        IClipRepository repository,
        SettingsService settings,
        ILogger<ClipboardService> log)
    {
        _capture = capture;
        _repository = repository;
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

        // The source app is read now, while the copy is fresh; the text read can sleep on retries,
        // so it runs off the UI thread.
        var appName = ForegroundApp.Name();
        _ = CaptureAsync(appName);
    }

    private async Task CaptureAsync(string? appName)
    {
        try
        {
            var text = await Task.Run(Win32Clipboard.TryReadText);
            if (text is null) return;

            await _capture.CaptureAsync(
                text,
                appName,
                DateTimeOffset.UtcNow,
                _settings.Current.MaxItems,
                CancellationToken.None);

            // Age-based cleanup rides along with captures so a long session stays trimmed.
            var days = _settings.Current.RetentionDays;
            if (days > 0)
                await _repository.PruneOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-days), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Capturing a clipboard change failed");
        }
    }
}
