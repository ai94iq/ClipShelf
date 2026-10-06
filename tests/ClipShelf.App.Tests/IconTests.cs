using ClipShelf.App.Resources;

namespace ClipShelf.App.Tests;

// A misspelled Kind="..." only fails when the screen opens, so check every usage here.
public sealed class IconTests
{
    private static readonly Regex KindUsage = new(
        @"AppIcon\b[^>]*?\sKind=""(?<k>\w+)""|IconData\s+Kind=(?<k>\w+)", RegexOptions.Compiled);

    [Fact]
    public void Every_icon_used_in_xaml_exists()
    {
        var unknown = TestPaths.SourceFiles(".xaml")
            .SelectMany(f => KindUsage.Matches(File.ReadAllText(f)).Select(m => m.Groups["k"].Value))
            .Where(k => !Enum.TryParse<IconKind>(k, out _))
            .Distinct();
        Assert.Empty(unknown);
    }

    [Fact]
    public void Every_icon_has_path_data() =>
        Assert.All(Enum.GetValues<IconKind>(), k => Assert.StartsWith("F", IconData.Get(k), StringComparison.Ordinal));
}
