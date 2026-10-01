namespace ClipShelf.Core.Models;

// Keyset paging position: the sort key and Id of the last row already shown.
public sealed record PageCursor(string Key, long Id);
