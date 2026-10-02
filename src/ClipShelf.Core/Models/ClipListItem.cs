namespace ClipShelf.Core.Models;

// A history row. Text is the full clip (empty for image clips); the view shows a preview or thumbnail.
public sealed record ClipListItem(
    long Id,
    string Text,
    string? AppName,
    bool IsPinned,
    DateTimeOffset CreatedAtUtc,
    PageCursor Cursor,
    long? CategoryId = null,
    string? CategoryName = null,
    byte[]? ThumbnailBytes = null)
{
    public bool HasImage => ThumbnailBytes is not null;
}
