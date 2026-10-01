namespace ClipShelf.Core.Models;

// A named group of saved clips; its clips survive Clear, the history limit and retention.
public sealed record Category(long Id, string Name);
