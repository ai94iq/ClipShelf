namespace ClipShelf.Core.Tests;

public sealed class ArabicTextTests
{
    [Theory]
    [InlineData("أحمد", "احمد")]
    [InlineData("مُحَمَّد", "محمد")]
    [InlineData("فاطمة", "فاطمه")]
    [InlineData("مصطفى", "مصطفي")]
    [InlineData("  Ali  ", "ali")]
    [InlineData("", "")]
    public void NormalizeForSearch_unifies_spelling_variants(string input, string expected) =>
        Assert.Equal(expected, ArabicText.NormalizeForSearch(input));
}
