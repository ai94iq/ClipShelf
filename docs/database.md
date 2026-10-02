# Database

File: `%LOCALAPPDATA%\ClipShelf\data.db`. WAL mode. Per-connection settings: synchronous=NORMAL, busy_timeout=5000, cache_size=-20000, foreign_keys=ON.

## Encryption

The database is SQLCipher-encrypted (see [decisions/0003](decisions/0003-encrypted-database.md)). The
32-byte key lives in `key.bin` next to it, protected with Windows DPAPI for the current user. An
existing plaintext `data.db` is encrypted with `sqlcipher_export()` on the next launch; the plaintext
file is replaced only after the export succeeds, and older plaintext backups are removed in the same
step. Backups (`VACUUM INTO`) are encrypted with the same key.

## Tables

| Table | Purpose | Key columns |
|---|---|---|
| AppMeta | Key/value metadata | Key |
| Clip | Clipboard history items | Id, TextHash (unique), IsPinned, CreatedAtUtc |

## Indexes

| Index | Columns | Used by |
|---|---|---|
| UX_Clip_TextHash | TextHash (unique) | Upsert on re-copy (moves item to the top) |
| IX_Clip_CreatedAtUtc | CreatedAtUtc DESC, Id DESC | Newest-first listing and search ordering |
| IX_Clip_Prune | CreatedAtUtc DESC, Id DESC WHERE IsPinned = 0 AND CategoryId IS NULL (partial) | History-limit trim and retention; keeps them proportional to the limit, not the history size |

## Migrations

| Version | File | Summary | Date |
|---|---|---|---|
| 0001 | 0001_initial.sql | AppMeta table | 2026-10-01 |
| 0002 | 0002_clip.sql | Clip table for clipboard history | 2026-10-01 |
| 0003 | 0003_category.sql | Category table; clips can be assigned to one | 2026-10-01 |
| 0004 | 0004_category_lock.sql | Category locks (password-protected) | 2026-10-01 |
| 0005 | 0005_category_lock_flag.sql | Shared password: category stores only its locked flag | 2026-10-01 |
| 0006 | 0006_image_clip.sql | Image clips: ImageBytes and ThumbnailBytes on Clip | 2026-10-02 |
| 0007 | 0007_prune_index.sql | Partial index for the history-limit trim | 2026-10-02 |

## Backups

`VACUUM INTO` before every migration and at most once a day; the newest 14 are kept in `backups\`.
