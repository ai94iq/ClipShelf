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
}
