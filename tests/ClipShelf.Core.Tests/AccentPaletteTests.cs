using ClipShelf.Core.Theming;

namespace ClipShelf.Core.Tests;

public sealed class AccentPaletteTests
{
    [Fact]
    public void Shades_get_lighter_and_darker_from_the_base()
    {
        var shades = AccentPalette.Shades("#0F6CBD");
        Assert.Equal("#0F6CBD", shades.Base);
        Assert.Equal("#3F89CA", shades.Light1);
        Assert.Equal("#0C5697", shades.Dark1);
    }

    [Fact]
    public void Every_preset_is_a_valid_hex_color() =>
        Assert.All(AccentPresets.All, p => AccentPalette.Parse(p.Hex));

    [Fact]
    public void Invalid_hex_is_rejected() =>
        Assert.Throws<FormatException>(() => AccentPalette.Parse("blue"));
}
