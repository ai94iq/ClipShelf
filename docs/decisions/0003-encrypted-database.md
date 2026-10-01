# 0003. Encrypted SQLite database

Date: 2026-10-01
Status: Accepted

## Context

The clipboard history holds whatever the user copies, including text from password managers and
private messages. A plaintext SQLite file under `%LOCALAPPDATA%` is readable by any process or
account that can open the file. The history also needed age-based retention and clearing at
sign-out without changing the storage engine.

## Decision

Use `SQLitePCLRaw.bundle_e_sqlcipher` with `Microsoft.Data.Sqlite.Core`, so every database file is
SQLCipher-encrypted with a 32-byte random key. The key is stored in `%LOCALAPPDATA%\ClipShelf\key.bin`,
protected with Windows DPAPI (`DataProtectionScope.CurrentUser`), and passed to the factory through
`DataOptions.EncryptionKey`. An existing plaintext `data.db` is re-written through
`sqlcipher_export()` on the next launch, keeping the original until the export succeeds.

## Consequences

- The database and its backups are unreadable without the key; the key is bound to the Windows
  user account. Deleting `key.bin` makes the existing history permanently unreadable.
- SQLCipher runs its key derivation when a physical connection is opened; connection pooling keeps
  this to a handful of opens per process.
- Restoring a backup on another machine or user account requires the matching `key.bin`.
- Tests stay unencrypted by default; encryption is covered by dedicated repository and migration tests.
