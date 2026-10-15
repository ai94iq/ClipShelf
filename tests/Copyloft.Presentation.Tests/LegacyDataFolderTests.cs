using Copyloft.Presentation.Hosting;

namespace Copyloft.Presentation.Tests;

public sealed class LegacyDataFolderTests : IDisposable
{
    private readonly string _parent = Directory.CreateTempSubdirectory("copyloft-paths").FullName;

    public void Dispose() => Directory.Delete(_parent, recursive: true);

    private string Root => Path.Combine(_parent, "Copyloft");
    private string Legacy => Path.Combine(_parent, "ClipShelf");

    [Fact]
    public void A_ClipShelf_folder_moves_to_the_Copyloft_folder_with_its_files()
    {
        Directory.CreateDirectory(Legacy);
        File.WriteAllText(Path.Combine(Legacy, "key.bin"), "key");

        Assert.Equal(Root, LegacyDataFolder.MoveTo(Root));
        Assert.False(Directory.Exists(Legacy));
        Assert.Equal("key", File.ReadAllText(Path.Combine(Root, "key.bin")));
    }

    [Fact]
    public void An_existing_Copyloft_folder_is_kept_and_the_ClipShelf_folder_left_alone()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Legacy);

        Assert.Equal(Root, LegacyDataFolder.MoveTo(Root));
        Assert.True(Directory.Exists(Legacy));
    }

    [Fact]
    public void Without_a_ClipShelf_folder_the_Copyloft_folder_is_used()
    {
        Assert.Equal(Root, LegacyDataFolder.MoveTo(Root));
        Assert.False(Directory.Exists(Root));
    }

    [Fact]
    public void A_ClipShelf_folder_held_open_is_used_in_place()
    {
        if (!OperatingSystem.IsWindows()) return; // only Windows refuses to move a folder with an open file
        Directory.CreateDirectory(Legacy);
        using var open = File.Open(Path.Combine(Legacy, "data.db"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Assert.Equal(Legacy, LegacyDataFolder.MoveTo(Root));
    }
}
