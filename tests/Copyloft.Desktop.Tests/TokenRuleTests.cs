using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

using Copyloft.Desktop.Services;

namespace Copyloft.Desktop.Tests;

// Fluent is the only token source; Copyloft owns just the exceptions block.
public sealed class TokenRuleTests
{
    private static readonly string[] ExceptionKeys = ["CopyloftAccentSubtleBrush", "CopyloftCardShadow"];

    private static readonly string Src = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Copyloft.Desktop"));

    // Every {DynamicResource}/{StaticResource} key used by any Desktop .axaml.
    public static IEnumerable<string> ReferencedKeys() =>
        Directory.EnumerateFiles(Src, "*.axaml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\{(?:Dynamic|Static)Resource\s+(\w+)\}"))
            .Select(m => m.Groups[1].Value)
            .Distinct();

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("Black")]
    public Task Every_referenced_key_resolves(string variant) => Ui.Run(() =>
    {
        var theme = variant switch { "Light" => ThemeVariant.Light, "Dark" => ThemeVariant.Dark, _ => ThemeService.Black };
        foreach (var key in ReferencedKeys().Concat(ExceptionKeys))
            Assert.True(Application.Current!.TryGetResource(key, theme, out _), $"{key} missing in {variant}");
    });

    [Fact]
    public Task The_accent_tint_flips_with_the_theme() => Ui.Run(() =>
    {
        Application.Current!.TryGetResource("CopyloftAccentSubtleBrush", ThemeVariant.Light, out var light);
        Application.Current!.TryGetResource("CopyloftAccentSubtleBrush", ThemeVariant.Dark, out var dark);
        var lightBrush = Assert.IsType<ISolidColorBrush>(light, exactMatch: false);
        var darkBrush = Assert.IsType<ISolidColorBrush>(dark, exactMatch: false);
        Assert.NotEqual((lightBrush.Color, lightBrush.Opacity), (darkBrush.Color, darkBrush.Opacity));
    });

    [Fact]
    public void Literal_colors_live_only_in_the_exceptions_file()
    {
        var offenders = Directory.EnumerateFiles(Src, "*.axaml", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "Exceptions.axaml")
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "#[0-9A-Fa-f]{3,8}\\b"))
            .Select(Path.GetFileName);
        Assert.Empty(offenders);
    }

    private static Color ColorOf(string key, ThemeVariant theme)
    {
        Assert.True(Application.Current!.TryGetResource(key, theme, out var value), $"{key} missing");
        return Assert.IsType<ISolidColorBrush>(value, exactMatch: false).Color;
    }

    // Fluent's dark transient border is near black, so card and group edges vanished on a dark
    // canvas (seen on Windows). Dark gets a faint white edge; Black keeps its gray one.
    [Fact]
    public Task Card_edges_show_in_dark_and_black() => Ui.Run(() =>
    {
        var dark = ColorOf("SystemControlTransientBorderBrush", ThemeVariant.Dark);
        Assert.Equal((255, 255, 255), (dark.R, dark.G, dark.B));
        Assert.InRange(dark.A, 0x18, 0x30);
        Assert.Equal(Color.Parse("#3A3A3A"), ColorOf("SystemControlTransientBorderBrush", ThemeService.Black));
    });

    // Solid dark cards hid the backdrop, so Mica and Acrylic looked the same (seen on Windows).
    // Dark cards are WinUI's 5% white wash: over the opaque canvas they still read #2B2B2B.
    [Fact]
    public Task Dark_cards_let_the_backdrop_through_and_look_the_same_when_opaque() => Ui.Run(() =>
    {
        var card = ColorOf("CopyloftCardBrush", ThemeVariant.Dark);
        var canvas = ColorOf("CopyloftCanvasBrush", ThemeVariant.Dark);
        Assert.InRange(card.A, 1, 0x1F);
        byte Over(byte top, byte bottom) => (byte)Math.Round(top * card.A / 255.0 + bottom * (1 - card.A / 255.0));
        var composite = Color.FromRgb(Over(card.R, canvas.R), Over(card.G, canvas.G), Over(card.B, canvas.B));
        Assert.InRange(composite.R, 0x2A, 0x2C);
        Assert.Equal(255, ColorOf("CopyloftCardBrush", ThemeVariant.Light).A);
        Assert.Equal(255, ColorOf("CopyloftCardBrush", ThemeService.Black).A);
    });

    // BaseMedium is 60% ink: hints and counts read as half transparent, worst in Arabic's thin
    // strokes (seen on Windows). Text takes BaseMediumHigh; icons may still use BaseMedium.
    [Fact]
    public void Secondary_text_is_not_the_faint_60_percent_ink()
    {
        const string faint = "SystemControlForegroundBaseMediumBrush";
        var offenders = Directory.EnumerateFiles(Src, "*.axaml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f),
                    @"<TextBlock\b[^>]*" + faint + @"|<Style Selector=""TextBlock[^""]*"">(?:(?!</Style>).)*" + faint +
                    @"|<ControlTemplate>(?:(?!</ControlTemplate>).)*<TextBlock\b[^>]*" + faint,
                    RegexOptions.Singleline)
                .Select(_ => Path.GetFileName(f)));
        Assert.Empty(offenders);
    }

    // AppIcon styles as PathIcon, so a selector naming AppIcon never matches and its setters are
    // silently dropped (row icons once rendered at 20 px instead of 16).
    [Fact]
    public void Style_selectors_name_PathIcon_not_AppIcon()
    {
        var offenders = Directory.EnumerateFiles(Src, "*.axaml", SearchOption.AllDirectories)
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "Selector=\"[^\"]*AppIcon"));
        Assert.Empty(offenders);
    }
}
