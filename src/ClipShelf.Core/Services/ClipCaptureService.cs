using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Text;

namespace ClipShelf.Core.Services;

// Turns raw clipboard text into history entries and keeps the list within bounds.
public sealed class ClipCaptureService(IClipRepository repository)
{
    public async Task<bool> CaptureAsync(
        string? text, string? appName, DateTimeOffset copiedAtUtc, int keepUnpinned, CancellationToken ct)
    {
        if (!ClipText.IsUsable(text)) return false;

        await repository.AddOrBumpAsync(text!, appName, copiedAtUtc, ct).ConfigureAwait(false);
        await repository.PruneAsync(keepUnpinned, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> CaptureImageAsync(
        byte[] image, byte[] thumbnail, string? appName, DateTimeOffset copiedAtUtc, int keepUnpinned, CancellationToken ct)
    {
        if (image.Length == 0) return false;

        await repository.AddImageAsync(image, thumbnail, appName, copiedAtUtc, ct).ConfigureAwait(false);
        await repository.PruneAsync(keepUnpinned, ct).ConfigureAwait(false);
        return true;
    }
}
