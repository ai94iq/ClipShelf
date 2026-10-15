using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

using ClipShelf.Desktop.Services;

namespace ClipShelf.Desktop.Tests;

// Fluent is the only token source; ClipShelf owns just the exceptions block.
public sealed class TokenRuleTests
{
    private static readonly string[] ExceptionKeys = ["ClipShelfAccentSubtleBrush", "ClipShelfCardShadow"];

    private static readonly string Src = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "ClipShelf.Desktop"));

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
        Application.Current!.TryGetResource("ClipShelfAccentSubtleBrush", ThemeVariant.Light, out var light);
        Application.Current!.TryGetResource("ClipShelfAccentSubtleBrush", ThemeVariant.Dark, out var dark);
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
