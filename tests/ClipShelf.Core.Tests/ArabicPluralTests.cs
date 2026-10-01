namespace ClipShelf.Core.Tests;

public sealed class ArabicPluralTests
{
    [Theory]
    [InlineData(0L, "zero")]
    [InlineData(1L, "one")]
    [InlineData(2L, "two")]
    [InlineData(3L, "few")]
    [InlineData(10L, "few")]
    [InlineData(11L, "many")]
    [InlineData(99L, "many")]
    [InlineData(100L, "other")]
    [InlineData(101L, "other")]
    [InlineData(103L, "few")]
    [InlineData(111L, "many")]
    public void Form_follows_cldr_rules(long count, string expected) =>
        Assert.Equal(expected, ArabicPlural.Form(count));
}
