using Avalonia;
using Avalonia.Media;
using ClipShelf.Desktop.Services;

namespace ClipShelf.Desktop.Tests;

// Main's app font: the Segoe UI Variable stack, or the bundled Noto Sans Arabic in an Arabic UI.
public sealed class AppFontsTests
{
    [Fact]
    public Task The_bundled_arabic_font_loads() => Ui.Run(() =>
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(AppFonts.Family(rtl: true)), out var glyphs));
        Assert.Equal("Noto Sans Arabic", glyphs!.FamilyName);
    });

    [Fact]
    public Task Latin_text_prefers_segoe_ui_variable() => Ui.Run(() =>
        Assert.Equal("Segoe UI Variable Text", AppFonts.Family(rtl: false).FamilyNames.PrimaryFamilyName));

    [Fact]
    public Task The_app_font_is_the_controls_theme_font() => Ui.Run(() =>
    {
        Assert.True(Application.Current!.TryGetResource("ContentControlThemeFontFamily", null, out var family));
        Assert.Equal(AppFonts.Family(rtl: false), family);
    });
}
