using ClipShelf.Core.Models;

namespace ClipShelf.Core.Abstractions;

// Clipboard history storage. Re-adding text that is already stored moves it to the top.
public interface IClipRepository
{
    Task AddOrBumpAsync(string text, string? appName, DateTimeOffset copiedAtUtc, CancellationToken ct);

    Task<IReadOnlyList<ClipListItem>> GetRecentAsync(PageCursor? after, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<ClipListItem>> SearchAsync(string query, PageCursor? after, int pageSize, CancellationToken ct);

    Task SetPinnedAsync(long id, bool pinned, CancellationToken ct);

    Task DeleteAsync(long id, CancellationToken ct);

    Task ClearUnpinnedAsync(CancellationToken ct);

    // Keeps the newest unpinned clips and every pinned clip; returns how many were removed.
    Task<int> PruneAsync(int keepUnpinned, CancellationToken ct);

    // Removes unpinned clips older than the cutoff; pinned clips are kept.
    Task<int> PruneOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken ct);

    // Session end cannot wait for a task; this clears unpinned clips synchronously.
    int ClearUnpinned();
}
