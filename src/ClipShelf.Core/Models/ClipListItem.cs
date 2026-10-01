namespace ClipShelf.Core.Models;

// A history row. Text is the full clip; the view shows ClipText.Preview(Text).
public sealed record ClipListItem(
    long Id,
    string Text,
    string? AppName,
    bool IsPinned,
    DateTimeOffset CreatedAtUtc,
    PageCursor Cursor,
    long? CategoryId = null,
    string? CategoryName = null);
