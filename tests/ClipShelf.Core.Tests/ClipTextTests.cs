namespace ClipShelf.Core.Tests;

public sealed class ClipTextTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   \r\n  ", false)]
    [InlineData("hello", true)]
    public void IsUsable_rejects_blank_text(string? text, bool expected) =>
        Assert.Equal(expected, ClipText.IsUsable(text));

    // WSLg's clipboard bridge offers a copied bitmap as UTF8_STRING: "BM", size bytes, zeros. Real
    // text never carries U+0000, so binary posing as text is not captured.
    [Fact]
    public void IsUsable_rejects_binary_posing_as_text() =>
        Assert.False(ClipText.IsUsable("BMf6\0\0\0\0\0\06\0\0\0(\0\0\0"));

    [Fact]
    public void IsUsable_rejects_text_over_the_limit() =>
        Assert.False(ClipText.IsUsable(new string('a', ClipText.MaxLength + 1)));

    [Fact]
    public void Hash_ignores_surrounding_whitespace_and_line_ending_style() =>
        Assert.Equal(ClipText.Hash("a\nb"), ClipText.Hash("  a\r\nb  "));

    [Fact]
    public void Hash_differs_for_different_text() =>
        Assert.NotEqual(ClipText.Hash("a"), ClipText.Hash("b"));

    [Fact]
    public void Preview_flattens_lines_and_truncates()
    {
        Assert.Equal("a b", ClipText.Preview("a\r\nb"));
        Assert.Equal("aaa…", ClipText.Preview("aaaaaa", maxLength: 3));
    }
}
