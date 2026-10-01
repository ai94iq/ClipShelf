namespace ClipShelf.Data;

// EncryptionKey is the SQLCipher passphrase; null keeps the database unencrypted (tests).
public sealed record DataOptions(string DatabasePath, string BackupDirectory, string? EncryptionKey = null);
