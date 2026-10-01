namespace ClipShelf.Data.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public void Migrations_are_applied()
    {
        using var db = new TempDatabase();
        using var connection = db.Factory.Open();
        Assert.True(connection.ExecuteScalar<long>("PRAGMA user_version;") >= 1);
    }

    [Fact]
    public void Second_start_writes_a_daily_backup()
    {
        using var db = new TempDatabase();
        db.Initializer().BackupAndMigrate();
        Assert.Single(Directory.GetFiles(db.Options.BackupDirectory, "data-*-daily.db"));
    }
}
