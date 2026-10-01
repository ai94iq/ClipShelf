namespace ClipShelf.Core.Abstractions;

// Sent after a successful write so open pages can reload. Area examples: "person", "union".
public sealed record DataChanged(string Area, long? Id = null);
