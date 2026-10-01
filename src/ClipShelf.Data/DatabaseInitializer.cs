namespace ClipShelf.Data;

public sealed class DatabaseInitializer(
    SqliteConnectionFactory factory, DataOptions options, ILogger<DatabaseInitializer> log)
{
    private const string Prefix = "Migrations.";
    private const int BackupsToKeep = 14;

    // Runs on a background thread at startup: encrypt an existing plaintext file, backup (if
    // due), then pending migrations.
    public void BackupAndMigrate()
    {
        Directory.CreateDirectory(options.BackupDirectory);
        if (options.EncryptionKey is not null && IsPlaintext(options.DatabasePath))
            EncryptExistingDatabase();

        var migrations = LoadMigrations();
        using var connection = factory.Open();
        connection.Execute("PRAGMA journal_mode = WAL;");
        var current = connection.ExecuteScalar<long>("PRAGMA user_version;");
        var pending = migrations.Where(m => m.Version > current).ToList();

        if (current > 0 && (pending.Count > 0 || BackupIsDue()))
            Backup(connection, pending.Count > 0 ? "pre-migration" : "daily");

        foreach (var migration in pending)
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(migration.Sql, transaction: transaction);
            // PRAGMA can't take parameters; the version comes from our own file name.
            connection.Execute(
                string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {migration.Version};"),
                transaction: transaction);
            transaction.Commit();
            log.LogInformation("Applied migration {Version} ({Name})", migration.Version, migration.Name);
        }
    }

    // SQLCipher writes a random header, so the SQLite magic string means the file is plaintext.
    private static bool IsPlaintext(string path)
    {
        if (!File.Exists(path)) return false;

        // Read-share so a pooled connection in this or another process does not block the check.
        using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var header = new byte[16];
        return stream.Read(header, 0, header.Length) == header.Length &&
               header.SequenceEqual("SQLite format 3\0"u8.ToArray());
    }

    // Moves an existing plaintext database into an encrypted copy with sqlcipher_export, then
    // replaces the original. The original stays untouched unless the export succeeds.
    private void EncryptExistingDatabase()
    {
        var database = options.DatabasePath;
        var target = database + ".encrypted";
        if (File.Exists(target)) File.Delete(target);

        using (var connection = new SqliteConnection($"Data Source={database}"))
        {
            connection.Open();
            var version = connection.ExecuteScalar<long>("PRAGMA user_version;");
            var attach = string.Create(CultureInfo.InvariantCulture,
                $"ATTACH DATABASE '{target.Replace("'", "''")}' AS encrypted KEY '{options.EncryptionKey!.Replace("'", "''")}';");
            connection.Execute(attach);
            connection.Execute("SELECT sqlcipher_export('encrypted');");
            // sqlcipher_export copies schema and data but not user_version.
            connection.Execute(string.Create(CultureInfo.InvariantCulture,
                $"PRAGMA encrypted.user_version = {version};"));
            connection.Execute("DETACH DATABASE encrypted;");
        }

        SqliteConnection.ClearAllPools();
        File.Move(target, database, overwrite: true);
        File.Delete(database + "-wal");   // plaintext sidecars must not survive
        File.Delete(database + "-shm");

        // Plaintext backups would keep readable copies of the history around.
        foreach (var backup in BackupFiles().ToList()) backup.Delete();

        log.LogInformation("Encrypted the existing database");
    }

    private void Backup(SqliteConnection connection, string reason)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var file = Path.Combine(options.BackupDirectory, $"data-{stamp}-{reason}.db");
        connection.Execute("VACUUM INTO @file;", new { file });
        log.LogInformation("Database backup written ({Reason})", reason);

        foreach (var old in BackupFiles().OrderByDescending(f => f.Name).Skip(BackupsToKeep))
            old.Delete();
    }

    private bool BackupIsDue()
    {
        var newest = BackupFiles().MaxBy(f => f.LastWriteTimeUtc);
        return newest is null || DateTime.UtcNow - newest.LastWriteTimeUtc > TimeSpan.FromDays(1);
    }

    private IEnumerable<FileInfo> BackupFiles() =>
        new DirectoryInfo(options.BackupDirectory).EnumerateFiles("data-*.db");

    private static List<Migration> LoadMigrations()
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal))
            .Select(n =>
            {
                var name = n[Prefix.Length..];                 // e.g. "0001_initial.sql"
                using var reader = new StreamReader(assembly.GetManifestResourceStream(n)!);
                return new Migration(int.Parse(name[..4], CultureInfo.InvariantCulture), name, reader.ReadToEnd());
            })
            .OrderBy(m => m.Version)
            .ToList();
    }

    private sealed record Migration(int Version, string Name, string Sql);
}
