namespace ClipShelf.Data;

public sealed class DatabaseInitializer(
    SqliteConnectionFactory factory, DataOptions options, ILogger<DatabaseInitializer> log)
{
    private const string Prefix = "Migrations.";
    private const int BackupsToKeep = 14;

    // Runs on a background thread at startup: backup (if due), then pending migrations.
    public void BackupAndMigrate()
    {
        Directory.CreateDirectory(options.BackupDirectory);
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
