using ClipShelf.Core.Services;
using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Windows;

// Turns clipboard changes into history entries and writes items back. Writes made by this app are
// counted so the resulting clipboard-changed message does not get captured as a new item.
[SupportedOSPlatform("windows")]
public sealed class ClipboardService : IClipboardMonitor, IDisposable
{
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

    public async Task WriteImageAsync(byte[] png)
    {
        _selfWrites++;
        var written = await Task.Run(() =>
            ImageClipCodec.ToDib(png) is { } dib && Win32Clipboard.TryWriteImage(dib));
        if (written) return;

        _selfWrites--;
        _log.LogWarning("Writing an image to the clipboard failed");
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }

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

            // Age-based cleanup rides along with captures so a long session stays trimmed.
            await PruneByAgeAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Capturing a clipboard change failed");
        }
    }

    private async Task CaptureImageAsync(byte[] dib, string? appName)
    {
        // Decoding runs on a worker thread: the imaging calls must never touch the UI thread.
        var png = await Task.Run(() => ImageClipCodec.ToPng(dib));
        if (png is null)
        {
            _log.LogWarning("An image copy could not be decoded ({Bytes} bytes); it was not saved", dib.Length);
            return;
        }

        var thumbnail = await Task.Run(() => ImageClipCodec.Thumbnail(png)) ?? png;
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
