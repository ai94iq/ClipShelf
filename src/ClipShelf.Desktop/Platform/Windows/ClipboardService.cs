using ClipShelf.Core.Services;
using ClipShelf.Desktop.Imaging;

namespace ClipShelf.Desktop.Platform.Windows;

// Turns clipboard changes into history entries and writes items back. Writes made by this app are
// counted so the resulting clipboard-changed message does not get captured as a new item.
public sealed class ClipboardService : IClipboardMonitor, IDisposable
{
    private const int ThumbnailMaxEdge = 256;

    private readonly ClipCaptureService _capture;
    private readonly IClipRepository _repository;
    private readonly SettingsService _settings;
    private readonly IForegroundApp _foregroundApp;
    private readonly ILogger<ClipboardService> _log;
    private readonly ClipboardWatcher _watcher = new();
    private int _selfWrites;

    public ClipboardService(
        ClipCaptureService capture,
        IClipRepository repository,
        SettingsService settings,
        IForegroundApp foregroundApp,
        ILogger<ClipboardService> log)
    {
        _capture = capture;
        _repository = repository;
        _settings = settings;
        _foregroundApp = foregroundApp;
        _log = log;
        _watcher.Changed += OnClipboardChanged;
    }

    public event EventHandler? Changed;

    public string? ReadText() => Win32Clipboard.TryReadText();

    public byte[]? ReadImage() => Win32Clipboard.TryReadImage();

    public void WriteText(string text)
    {
        _selfWrites++;
        if (Win32Clipboard.TryWriteText(text)) return;

        _selfWrites--;
        _log.LogWarning("Writing text to the clipboard failed");
    }

    public Task WriteImageAsync(byte[] png)
    {
        _selfWrites++;
        var dib = SkiaImageCodec.PngToDib(png);
        if (dib is null || !Win32Clipboard.TryWriteImage(dib))
        {
            _selfWrites--;
            _log.LogWarning("Writing an image to the clipboard failed");
        }

        return Task.CompletedTask;
    }

    public void Dispose() => _watcher.Dispose();

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        if (_selfWrites > 0)
        {
            _selfWrites--;
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);

        // The source app is read now, while the copy is fresh; the text read can sleep on retries,
        // so it runs off the UI thread.
        var appName = _foregroundApp.Name();
        _ = CaptureAsync(appName);
    }

    private async Task CaptureAsync(string? appName)
    {
        try
        {
            // An image copy wins over any text formats the same copy may carry.
            var dib = await Task.Run(() => Win32Clipboard.HasImage() ? Win32Clipboard.TryReadImage() : null);
            if (dib is not null)
            {
                await CaptureImageAsync(dib, appName);
                return;
            }

            var text = await Task.Run(Win32Clipboard.TryReadText);
            if (text is null) return;

            await _capture.CaptureAsync(
                text,
                appName,
                DateTimeOffset.UtcNow,
                _settings.Current.MaxItems,
                CancellationToken.None);

            await PruneByAgeAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Capturing a clipboard change failed");
        }
    }

    private async Task CaptureImageAsync(byte[] dib, string? appName)
    {
        var png = await Task.Run(() => SkiaImageCodec.ToPng(dib));
        if (png is null) return;

        var thumbnail = await Task.Run(() => SkiaImageCodec.Thumbnail(png, ThumbnailMaxEdge)) ?? png;
        await _capture.CaptureImageAsync(
            png,
            thumbnail,
            appName,
            DateTimeOffset.UtcNow,
            _settings.Current.MaxItems,
            CancellationToken.None);

        await PruneByAgeAsync();
    }

    private async Task PruneByAgeAsync()
    {
        var days = _settings.Current.RetentionDays;
        if (days > 0)
            await _repository.PruneOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-days), CancellationToken.None);
    }
}
