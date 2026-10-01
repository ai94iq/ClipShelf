using ClipShelf.App.Services;
using System.Text;

namespace ClipShelf.App.Tests.Settings;

public sealed class DatabaseKeyTests
{
    [Fact]
    public void Load_or_create_returns_the_same_key_on_the_second_call()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clipshelf-key-{Guid.NewGuid():N}.bin");
        try
        {
            var first = DatabaseKey.LoadOrCreate(path);
            var second = DatabaseKey.LoadOrCreate(path);

            Assert.Equal(first, second);
            Assert.Equal(64, first.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void The_key_file_does_not_store_the_key_as_text()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clipshelf-key-{Guid.NewGuid():N}.bin");
        try
        {
            var key = DatabaseKey.LoadOrCreate(path);

            var stored = File.ReadAllBytes(path);
            Assert.NotEqual(key, Encoding.UTF8.GetString(stored));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
