using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ClipShelf.Core.Theming;
using ClipShelf.Desktop.Services;
using ClipShelf.Desktop.Shell;
using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Desktop.Tests;

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
}
