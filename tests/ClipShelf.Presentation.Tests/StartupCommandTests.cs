using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Presentation.Tests;

public sealed class StartupCommandTests
{
    [Theory]
    [InlineData(new[] { "app.exe" }, null)]
    [InlineData(new[] { "app.exe", "--toggle" }, "--toggle")]
    [InlineData(new[] { "app.exe", "--history" }, "--history")]
    [InlineData(new[] { "app.exe", "--unknown" }, null)]
    public void Parse_reads_only_known_commands(string[] args, string? expected)
    {
        Assert.Equal(expected, StartupCommand.Parse(args));
    }
}
