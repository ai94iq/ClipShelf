using ClipShelf.App.Services;

namespace ClipShelf.App.Tests;

public sealed class UpdateCheckerTests
{
    [Theory]
    [InlineData("v0.3.0", "0.2.0", true)]
    [InlineData("0.3.0", "0.2.0", true)]
    [InlineData("v1.0.0", "0.9.9", true)]
    [InlineData("v0.2.0", "0.2.0", false)]
    [InlineData("v0.1.9", "0.2.0", false)]
    [InlineData("banana", "0.2.0", false)]
    [InlineData(null, "0.2.0", false)]
    public void Only_newer_release_tags_count(string? tag, string current, bool expected) =>
        Assert.Equal(expected, UpdateChecker.IsNewer(tag, current));
}
