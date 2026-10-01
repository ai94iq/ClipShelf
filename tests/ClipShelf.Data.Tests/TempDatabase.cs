namespace ClipShelf.Data.Tests;

// A real SQLite file with all migrations applied; deleted on Dispose.
public sealed class TempDatabase : IDisposable
{
    // A fixed 64-character key stands in for the DPAPI-protected one from the app.
    public const string Key = "0A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F9";

    public TempDatabase(string? encryptionKey = null)
    {
        var directory = Directory.CreateTempSubdirectory("ClipShelf-tests-").FullName;
        Options = new DataOptions(Path.Combine(directory, "test.db"), Path.Combine(directory, "backups"), encryptionKey);
        Factory = new SqliteConnectionFactory(Options);
        Initializer().BackupAndMigrate();
    }

    public DataOptions Options { get; }

    public SqliteConnectionFactory Factory { get; }

    public DatabaseInitializer Initializer() =>
        new(Factory, Options, NullLogger<DatabaseInitializer>.Instance);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();                          // release file handles
        Directory.Delete(Path.GetDirectoryName(Options.DatabasePath)!, recursive: true);
    }
}
