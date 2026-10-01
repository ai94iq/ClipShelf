namespace ClipShelf.Core.Models;

// A named group of saved clips; its clips survive Clear, the history limit and retention.
// A locked category hides its clips until the shared password unlocks it for the session.
public sealed record Category(long Id, string Name, bool IsLocked = false);
