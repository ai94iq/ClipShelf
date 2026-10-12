using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

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
    public Task Every_referenced_key_resolves(string variant) => Ui.Run(() =>
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
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
}
