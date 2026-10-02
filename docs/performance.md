# Performance

How ClipShelf behaves as the clipboard history grows. The numbers come from a load harness that
drives the real storage path — SQLCipher-encrypted SQLite through the Dapper repositories,
including the trim that runs on every copy — with warm pooled connections on a Windows 11 x64
machine (.NET 10). Each size starts from a fresh encrypted database, so the rows are independent.

Times are per operation in milliseconds unless noted. "Flyout page" is the 50-newest query the
flyout issues on open. "Search" is a substring scan; a hit and a miss cost the same, because
`LIKE '%text%'` has no index to stop early on.

## Scaling

| Clips | Insert /clip | Flyout page | Search | Capture + trim | Database |
|---|---|---|---|---|---|
| 50 | 0.20 | 0.50¹ | 0.54 | 0.50 | 0.0 MB |
| 100 | 0.18 | 0.50 | 0.55 | 0.39 | 0.1 MB |
| 250 | 0.19 | 0.42 | 0.58 | 0.45 | 0.1 MB |
| 500 | 0.18 | 0.44 | 0.64 | 0.51 | 0.2 MB |
| 1,000 | 0.19 | 0.42 | 0.74 | 0.73 | 0.4 MB |
| 5,000 | 0.20 | 0.57 | 1.58 | 5.76 | 2.1 MB |
| 10,000 | 0.20 | 0.44 | 2.58 | 12.16 | 4.2 MB |

¹ First query of a fresh database (JIT and page warm-up); the steady state is about 0.5 ms.

- **Insert** is flat at about 0.2 ms per copy — 10,000 clips in roughly 2 seconds.
- **The flyout page** is a keyset query on `IX_Clip_CreatedAtUtc` and stays under a millisecond;
  the first query after opening a cold database pays a few milliseconds of page reads.
- **Search** grows linearly with the history; at the Settings maximum (1,000 clips) it stays
  within a few milliseconds.
- **Storage** is about 0.43 KB per text clip, encrypted.
- **The trim** was the one operation that grew with the whole history — it ran on every copy.
  It now uses the partial index `IX_Clip_Prune`, which covers only the clips the limit applies
  to (unpinned and uncategorized), so its cost tracks the limit, not the archive.

## One million clips

Measured before the trim fix, on a fresh encrypted database:

| Metric | Value |
|---|---|
| Insert | 252 s (0.25 ms per clip) |
| Flyout page | 20.4 ms average — 31 ms cold, 0.4 ms warm |
| Search | 3.85 s (hit) / 3.91 s (miss) |
| Capture + trim per copy | **7.61 s** |
| 16 concurrent copies and reads | 57.8 s, 2 failures (`SQLITE_BUSY: database is locked`) |
| Database | 429 MB |

The per-copy trim and the lock failures share one cause: the old `NOT IN` trim scanned the whole
table on every copy, held the write lock for seconds and made concurrent captures collide. After
the fix, a planner check confirms the partial index serves the trim
(`SCAN Clip USING INDEX IX_Clip_Prune`), and a 20,000-row probe measured the steady-state trim at
0.15–2.85 ms regardless of the configured limit. The one-million-row re-verification was stopped
early, so there is no full after-fix table for that size yet.

## What is not measured

- The Win32 clipboard read/write per copy is a constant cost (microseconds) that does not grow
  with the history.
- UI rendering and the flyout animation are not part of these numbers.
- Numbers are from one machine; absolute values vary with disk and CPU, the shapes (flat, linear,
  logarithmic) do not.
