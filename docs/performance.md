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

Two runs on fresh encrypted databases with a 1,000-item limit — before and after the trim fix:

| Metric | Before | After |
|---|---|---|
| Insert | 252 s (0.25 ms per clip) | 321 s (0.32 ms per clip)¹ |
| Flyout page | 20.4 ms average | 13.1 ms average¹ ² |
| Search | 3.85 s (hit) / 3.91 s (miss) | 2.37 s (hit) / 2.40 s (miss) |
| Capture + trim per copy | **7.61 s** | **1.26 ms** |
| 16 concurrent copies and reads | 57.8 s, 2 failures (`SQLITE_BUSY: database is locked`) | 2.6 s, 0 failures |
| Database | 429 MB | 520 MB³ |

¹ Insert and search are not affected by the fix; the spread between runs is machine variance.
² Includes the first, cold query after the bulk insert; repeated queries are much faster
(0.4 ms warm in the probe).
³ Includes the freed pages and WAL left by the one-time bulk trim; SQLite reuses them, and a
`VACUUM` would shrink the file.

The per-copy trim and the lock failures shared one cause: the old `NOT IN` trim scanned the whole
table on every copy, held the write lock for seconds and made concurrent captures collide. The
partial index `IX_Clip_Prune` serves the trim now, so its cost tracks the limit — 1.26 ms per
copy with a million clips stored — instead of the archive, and the planner confirms the index is
used (`SCAN Clip USING INDEX IX_Clip_Prune`).

Trimming a history that has already grown far past the limit is a one-time bulk delete: 999,001
rows in 59.7 s in the "after" run, in a single transaction. Steady state afterwards is back to
about a millisecond per copy.

## What is not measured

- The Win32 clipboard read/write per copy is a constant cost (microseconds) that does not grow
  with the history.
- UI rendering and the flyout animation are not part of these numbers.
- Numbers are from one machine; absolute values vary with disk and CPU, the shapes (flat, linear,
  logarithmic) do not.
