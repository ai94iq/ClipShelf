using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the theme tokens: the ClipShelf palette aliases resolve
// to Fluent's light and dark values, and applying dark settings flips the app
// variant.
[Collection("headless")]
[SupportedOSPlatform("windows")]
public sealed class ThemeTokensTests : IDisposable
{
    private readonly HeadlessApp _app = new();

    // The palette is Fluent's. The invariant is not a specific hex value, it
    // is that every alias resolves to a real brush and that the surface and
    // text tokens change between light and dark because FluentTheme supplies
    // both. Hardcoding hexes here would re-freeze the palette we just removed.
    public static TheoryData<string> PaletteKeys() =>
    [
        "SurfaceBackground", "SurfaceCard", "SurfaceElevated", "SurfaceOverlay",
        "TextPrimary", "TextSecondary", "TextMuted",
        "AccentBrush", "AccentSubtleBrush",
        "BorderSubtle", "BorderDefault", "DividerBrush",
        "HoverOverlay", "SidebarBackground", "ErrorBrush", "SuccessBrush",
    ];

    [Theory]
    [MemberData(nameof(PaletteKeys))]
    public void Tokens_ResolveToABrushInBothVariants(string key)
    {
        _app.Dispatch(() =>
        {
            var host = Application.Current!;
            Assert.NotNull(Brush(host, ThemeVariant.Light, key));
            Assert.NotNull(Brush(host, ThemeVariant.Dark, key));
        });
    }

    [Fact]
    public void Tokens_FollowFluentLightAndDark()
    {
        _app.Dispatch(() =>
        {
            var host = Application.Current!;
            foreach (var key in (string[])["SurfaceBackground", "SurfaceCard", "TextPrimary", "TextSecondary"])
            {
                var light = Brush(host, ThemeVariant.Light, key).Color;
                var dark = Brush(host, ThemeVariant.Dark, key).Color;
                Assert.True(light != dark, $"{key} did not change between light and dark.");
            }
        });
    }

    [Fact]
    public void Apply_DarkSettings_FlipsAppVariant()
    {
        _app.Dispatch(() =>
        {
            var service = new AvaloniaThemeService();
            service.Apply(new AppSettings { Theme = AppTheme.Dark });
            Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
            service.Apply(new AppSettings { Theme = AppTheme.Light });
            Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
        });
    }

    public void Dispose() => _app.Dispose();

    private static SolidColorBrush Brush(IResourceHost host, ThemeVariant variant, string key) =>
        Assert.IsType<SolidColorBrush>(host.FindResource(variant, key));
}
