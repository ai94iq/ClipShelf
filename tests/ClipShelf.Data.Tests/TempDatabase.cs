namespace ClipShelf.Data.Tests;

// A real SQLite file with all migrations applied; deleted on Dispose.
public sealed class TempDatabase : IDisposable
{
    public TempDatabase()
    {
        var directory = Directory.CreateTempSubdirectory("ClipShelf-tests-").FullName;
        Options = new DataOptions(Path.Combine(directory, "test.db"), Path.Combine(directory, "backups"));
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
