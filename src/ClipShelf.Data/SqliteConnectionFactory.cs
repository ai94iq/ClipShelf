namespace ClipShelf.Data;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(DataOptions options)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = options.DatabasePath,
            ForeignKeys = true,
            Pooling = true,
        };

        // Microsoft.Data.Sqlite issues PRAGMA key for SQLCipher builds before anything else,
        // and only once per physical connection, which keeps pooling correct.
        if (options.EncryptionKey is not null) builder.Password = options.EncryptionKey;

        _connectionString = builder.ToString();
    }

    // One connection per unit of work; pooling makes this cheap.
    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        // Per-connection settings; cache_size negative = KiB (~20 MB).
        connection.Execute("PRAGMA synchronous = NORMAL; PRAGMA busy_timeout = 5000; PRAGMA cache_size = -20000;");
        return connection;
    }
}
