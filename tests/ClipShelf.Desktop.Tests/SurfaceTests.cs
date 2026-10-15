using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ClipShelf.Desktop.Services;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Tests;

// Main's surfaces: Light is a soft gray canvas under white cards, Dark the Windows dark layers,
// full black is black throughout (borders keep the layers apart).
public sealed class SurfaceTests
{
    private static ThemeVariant Variant(string name) =>
        name switch { "Light" => ThemeVariant.Light, "Dark" => ThemeVariant.Dark, _ => ThemeService.Black };

    private static Color Resolve(string key, ThemeVariant variant)
    {
        Assert.True(Application.Current!.TryGetResource(key, variant, out var value), key);
        return Assert.IsType<ISolidColorBrush>(value, exactMatch: false).Color;
    }

    [Theory]
    [InlineData("Light", "#E6E6E6", "#FFFFFF")]
    [InlineData("Dark", "#202020", "#0DFFFFFF")]   // a white wash; reads #2B2B2B over the canvas
    [InlineData("Black", "#000000", "#000000")]
    public Task Canvas_and_card_follow_the_theme(string variant, string canvas, string card) => Ui.Run(() =>
    {
        Assert.Equal(Color.Parse(canvas), Resolve("ClipShelfCanvasBrush", Variant(variant)));
        Assert.Equal(Color.Parse(card), Resolve("ClipShelfCardBrush", Variant(variant)));
    });

    [Fact]
    public Task The_main_window_sits_on_the_canvas() => Ui.Run(() =>
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var window = new MainWindow(new AppLifetime());
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Color.Parse("#E6E6E6"), Assert.IsType<ISolidColorBrush>(window.Background, exactMatch: false).Color);
    });

    [Fact]
    public Task Settings_groups_are_cards() => Ui.Run(() =>
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;   // where card and window differ
        var kit = new SettingsKit();
        var window = new Window { Content = kit.Page };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var group = kit.Page.FindControl<StackPanel>("TrayGroup")!.Children.OfType<Border>().Single();
        Assert.Equal(Color.Parse("#0DFFFFFF"), Assert.IsType<ISolidColorBrush>(group.Background, exactMatch: false).Color);
    });

    // Full black cards sit on a black canvas: only their edge tells them apart, so it must show.
    [Fact]
    public Task Full_black_keeps_visible_card_edges() => Ui.Run(() =>
    {
        var edge = Resolve("SystemControlTransientBorderBrush", ThemeService.Black);

        Assert.Equal(255, edge.A);
        Assert.InRange(edge.R, 0x30, 0x50);
    });
}
