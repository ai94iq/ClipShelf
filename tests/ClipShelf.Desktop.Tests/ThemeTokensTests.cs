using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the theme tokens: the same keys resolve to light values
// under the Light variant and dark values under the Dark variant, and applying
// dark settings flips the app variant.
[Collection("headless")]
[SupportedOSPlatform("windows")]
public sealed class ThemeTokensTests : IDisposable
{
    private readonly HeadlessApp _app = new();

    [Fact]
    public void Tokens_ResolvePerThemeVariant()
    {
        _app.Dispatch(() =>
        {
            var host = Application.Current!;
            Assert.Equal(Color.Parse("#F5F5F7"), Brush(host, ThemeVariant.Light, "SurfaceBackground").Color);
            Assert.Equal(Color.Parse("#1C1C1E"), Brush(host, ThemeVariant.Dark, "SurfaceBackground").Color);
            Assert.Equal(Color.Parse("#FFFFFF"), Brush(host, ThemeVariant.Light, "SurfaceCard").Color);
            Assert.Equal(Color.Parse("#2C2C2E"), Brush(host, ThemeVariant.Dark, "SurfaceCard").Color);
            Assert.Equal(Color.Parse("#1A1A2E"), Brush(host, ThemeVariant.Light, "TextPrimary").Color);
            Assert.Equal(Color.Parse("#F5F5F7"), Brush(host, ThemeVariant.Dark, "TextPrimary").Color);
            Assert.Equal(Color.Parse("#3B82F6"), Brush(host, ThemeVariant.Light, "AccentBrush").Color);
            Assert.Equal(Color.Parse("#3B82F6"), Brush(host, ThemeVariant.Dark, "AccentBrush").Color);
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
