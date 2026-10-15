using Copyloft.Presentation.Hosting;

namespace Copyloft.Presentation.Tests;

// A running ClipShelf 0.4.x holds its single-instance name with the "clipshelf-" endpoint prefix.
public sealed class LegacyInstanceTests
{
    private static string NewName() => "ClipShelf.Test." + Guid.NewGuid().ToString("N");

    [Fact]
    public void A_claimed_ClipShelf_name_reads_as_running()
    {
        var name = NewName();
        using var clipShelf = new SingleInstanceService(name, "clipshelf-");
        Assert.True(clipShelf.TryClaim());

        Assert.True(LegacyInstance.IsRunning(name));
    }

    [Fact]
    public void A_free_ClipShelf_name_reads_as_not_running_and_stays_free()
    {
        var name = NewName();

        Assert.False(LegacyInstance.IsRunning(name));
        using var clipShelf = new SingleInstanceService(name, "clipshelf-");
        Assert.True(clipShelf.TryClaim());
    }

    [Fact]
    public void The_name_is_the_one_ClipShelf_claimed()
    {
        Assert.StartsWith("ClipShelf.SingleInstance." + Environment.UserName, LegacyInstance.Name);
        Assert.Equal(
            SingleInstanceService.DefaultName.Replace("Copyloft.", "ClipShelf.", StringComparison.Ordinal),
            LegacyInstance.Name);
    }
}
