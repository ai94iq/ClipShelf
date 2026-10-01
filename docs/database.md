# Database

File: `%LOCALAPPDATA%\ClipShelf\data.db`. WAL mode. Per-connection settings: synchronous=NORMAL, busy_timeout=5000, cache_size=-20000, foreign_keys=ON.

## Tables

| Table | Purpose | Key columns |
|---|---|---|
| AppMeta | Key/value metadata | Key |
| Clip | Clipboard history items | Id, TextHash (unique), IsPinned, CreatedAtUtc |

## Indexes

| Index | Columns | Used by |
|---|---|---|
| UX_Clip_TextHash | TextHash (unique) | Upsert on re-copy (moves item to the top) |
| IX_Clip_CreatedAtUtc | CreatedAtUtc DESC, Id DESC | Newest-first listing, search and prune |

## Migrations

| Version | File | Summary | Date |
|---|---|---|---|
| 0001 | 0001_initial.sql | AppMeta table | 2026-10-01 |
| 0002 | 0002_clip.sql | Clip table for clipboard history | 2026-10-01 |

## Backups

`VACUUM INTO` before every migration and at most once a day; the newest 14 are kept in `backups\`.
