namespace ClipShelf.Data.Tests;

public sealed class DatabaseEncryptionTests
{
    [Fact]
    public void An_encrypted_database_round_trips_and_hides_its_header()
    {
        using var db = new TempDatabase(TempDatabase.Key);

        using (var connection = db.Factory.Open())
        {
            connection.Execute("INSERT INTO Clip (Text, TextHash, CreatedAtUtc) VALUES ('secret', 'h1', '2026-01-01T00:00:00.0000000+00:00');");
        }

        using (var connection = db.Factory.Open())
        {
            Assert.Equal("secret", connection.ExecuteScalar<string>("SELECT Text FROM Clip;"));
        }

        SqliteConnection.ClearAllPools();   // release pooled handles before reading the raw file
        Assert.False(IsPlaintext(db.Options.DatabasePath));
    }

    [Fact]
    public void A_wrong_key_cannot_read_the_database()
    {
        using var db = new TempDatabase(TempDatabase.Key);
        using (var connection = db.Factory.Open())
            connection.Execute("INSERT INTO Clip (Text, TextHash, CreatedAtUtc) VALUES ('x', 'h', '2026-01-01T00:00:00.0000000+00:00');");

        var wrong = db.Options with { EncryptionKey = new string('F', 64) };

        Assert.ThrowsAny<Exception>(() =>
        {
            using var connection = new SqliteConnectionFactory(wrong).Open();
            connection.ExecuteScalar<string>("SELECT Text FROM Clip;");
        });
    }

    [Fact]
    public void An_existing_plaintext_database_is_encrypted_and_keeps_its_rows()
    {
        using var plain = new TempDatabase();
        using (var connection = plain.Factory.Open())
            connection.Execute("INSERT INTO Clip (Text, TextHash, CreatedAtUtc) VALUES ('kept', 'h1', '2026-01-01T00:00:00.0000000+00:00');");
        SqliteConnection.ClearAllPools();
        Assert.True(IsPlaintext(plain.Options.DatabasePath));

        var encryptedOptions = plain.Options with { EncryptionKey = TempDatabase.Key };
        var factory = new SqliteConnectionFactory(encryptedOptions);
        new DatabaseInitializer(factory, encryptedOptions, NullLogger<DatabaseInitializer>.Instance).BackupAndMigrate();

        using (var connection = factory.Open())
            Assert.Equal("kept", connection.ExecuteScalar<string>("SELECT Text FROM Clip;"));
        SqliteConnection.ClearAllPools();
        Assert.False(IsPlaintext(encryptedOptions.DatabasePath));
    }

    [Fact]
    public void Encrypting_removes_plaintext_backups()
    {
        using var plain = new TempDatabase();
        Directory.CreateDirectory(plain.Options.BackupDirectory);
        var staleBackup = Path.Combine(plain.Options.BackupDirectory, "data-20260101-000000-daily.db");
        File.WriteAllText(staleBackup, "plaintext copy");

        var encryptedOptions = plain.Options with { EncryptionKey = TempDatabase.Key };
        new DatabaseInitializer(new SqliteConnectionFactory(encryptedOptions), encryptedOptions, NullLogger<DatabaseInitializer>.Instance).BackupAndMigrate();

        Assert.False(File.Exists(staleBackup));
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(encryptedOptions.BackupDirectory, "data-*.db"))
            Assert.False(IsPlaintext(file));
    }

    private static bool IsPlaintext(string path)
    {
        using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var header = new byte[16];
        return stream.Read(header, 0, header.Length) == header.Length &&
               header.SequenceEqual("SQLite format 3\0"u8.ToArray());
    }
}
