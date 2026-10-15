using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Copyloft.Core.Theming;
using Copyloft.Desktop.Services;
using Copyloft.Desktop.Shell;
using Copyloft.Presentation.Hosting;

namespace Copyloft.Desktop.Tests;

// Main's rule: Light and full black are opaque; Dark and System show the chosen backdrop; the
// flyout is always acrylic. Where the backdrop is not available the window keeps its surface.
public sealed class WindowSurfaceTests
{
    private static Window Shown(Window window)
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static WindowTransparencyLevel[] Levels(string kind) => kind switch
    {
        "Mica" => [WindowTransparencyLevel.Mica],
        "Acrylic" => [WindowTransparencyLevel.AcrylicBlur],
        _ => [],
    };

    private static void With(AppSettings settings, Action body)
    {
        try
        {
            WindowSurface.Apply(settings);
            body();
        }
        finally
        {
            WindowSurface.Apply(null);
        }
    }

    [Theory]
    [InlineData(AppTheme.Dark, BackdropKind.Mica, "Mica")]
    [InlineData(AppTheme.System, BackdropKind.Acrylic, "Acrylic")]
    [InlineData(AppTheme.Dark, BackdropKind.None, "None")]
    [InlineData(AppTheme.Light, BackdropKind.Mica, "None")]
    [InlineData(AppTheme.Black, BackdropKind.Acrylic, "None")]
    public Task The_main_window_asks_for_the_backdrop_its_theme_allows(AppTheme theme, BackdropKind backdrop, string expected) => Ui.Run(() =>
        With(new AppSettings { Theme = theme, Backdrop = backdrop }, () =>
        {
            var window = Shown(new MainWindow(new AppLifetime()));

            Assert.Equal(Levels(expected), window.TransparencyLevelHint);
        }));

    [Fact]
    public Task The_flyout_is_always_acrylic() => Ui.Run(() =>
        With(new AppSettings { Theme = AppTheme.Light, Backdrop = BackdropKind.None }, () =>
        {
            var kit = new FlyoutTestKit();

            Assert.Equal(Levels("Acrylic"), kit.Window.TransparencyLevelHint);
        }));

    [Fact]
    public Task A_theme_change_reaches_open_windows() => Ui.Run(() =>
        With(new AppSettings { Theme = AppTheme.Dark, Backdrop = BackdropKind.Mica }, () =>
        {
            var window = Shown(new MainWindow(new AppLifetime()));

            WindowSurface.Apply(new AppSettings { Theme = AppTheme.Light, Backdrop = BackdropKind.Mica });

            Assert.Empty(window.TransparencyLevelHint);
        }));

    // Headless has no Mica, the same as Windows 10: the window must not turn see-through.
    [Fact]
    public Task Without_the_backdrop_the_window_keeps_its_canvas() => Ui.Run(() =>
        With(new AppSettings { Theme = AppTheme.Dark, Backdrop = BackdropKind.Mica }, () =>
        {
            Avalonia.Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
            var window = Shown(new MainWindow(new AppLifetime()));

            Assert.Equal(Color.Parse("#202020"), Assert.IsType<ISolidColorBrush>(window.Background, exactMatch: false).Color);
        }));

    // Live, an opaque window still reports "Transparent" (WinUI composition): only a real
    // backdrop may clear the surface, or Light shows black behind dark text.
    [Theory]
    [InlineData("Mica", true)]
    [InlineData("AcrylicBlur", true)]
    [InlineData("Blur", true)]
    [InlineData("Transparent", false)]
    [InlineData("None", false)]
    public void Only_a_real_backdrop_clears_the_surface(string level, bool seeThrough)
    {
        var actual = level switch
        {
            "Mica" => WindowTransparencyLevel.Mica,
            "AcrylicBlur" => WindowTransparencyLevel.AcrylicBlur,
            "Blur" => WindowTransparencyLevel.Blur,
            "Transparent" => WindowTransparencyLevel.Transparent,
            _ => WindowTransparencyLevel.None,
        };

        Assert.Equal(seeThrough, WindowSurface.ShowsBackdrop(actual));
    }

    // Avalonia's acrylic has no tint of its own: without the surface over it, Light text sat on
    // the dark desktop. Blur gets the surface at 80%; Mica is tinted by the system; else opaque.
    [Theory]
    [InlineData("AcrylicBlur", 0.8)]
    [InlineData("Blur", 0.8)]
    [InlineData("Mica", 0.0)]
    [InlineData("Transparent", 1.0)]
    [InlineData("None", 1.0)]
    public void The_surface_tints_each_backdrop(string level, double opacity)
    {
        var actual = level switch
        {
            "Mica" => WindowTransparencyLevel.Mica,
            "AcrylicBlur" => WindowTransparencyLevel.AcrylicBlur,
            "Blur" => WindowTransparencyLevel.Blur,
            "Transparent" => WindowTransparencyLevel.Transparent,
            _ => WindowTransparencyLevel.None,
        };

        Assert.Equal(opacity, WindowSurface.SurfaceOpacity(actual));
    }
}
