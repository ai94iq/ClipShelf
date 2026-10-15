using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Copyloft.Desktop.Services;
using Copyloft.Presentation.Hosting;

namespace Copyloft.Desktop.Tests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData(AppTheme.System, "Default")]
    [InlineData(AppTheme.Light, "Light")]
    [InlineData(AppTheme.White, "Light")]
    [InlineData(AppTheme.Dark, "Dark")]
    [InlineData(AppTheme.Black, "Black")]
    public Task The_app_theme_maps_to_a_variant(AppTheme theme, string expected) => Ui.Run(() =>
    {
        new ThemeService().Apply(new AppSettings { Theme = theme });
        Assert.Equal(expected, Application.Current!.RequestedThemeVariant!.Key.ToString());
    });

    [Fact]
    public Task A_preset_accent_sets_the_fluent_accent_color() => Ui.Run(() =>
    {
        new ThemeService().Apply(new AppSettings { Accent = "#0F766E" });
        Assert.Equal(Color.Parse("#0F766E"), Application.Current!.Resources["SystemAccentColor"]);
    });

    [Fact]
    public Task No_accent_hands_the_accent_back_to_the_system() => Ui.Run(() =>
    {
        var service = new ThemeService();
        service.Apply(new AppSettings { Accent = "#0F766E" });
        service.Apply(new AppSettings { Accent = null });
        Assert.False(Application.Current!.Resources.ContainsKey("SystemAccentColor"));
    });

    // The selected-row tint is the accent at low opacity, so it follows whatever accent is set.
    [Fact]
    public Task The_selection_tint_follows_the_accent() => Ui.Run(() =>
    {
        new ThemeService().Apply(new AppSettings { Theme = AppTheme.Light, Accent = "#0F766E" });
        Dispatcher.UIThread.RunJobs();

        Application.Current!.TryGetResource("CopyloftAccentSubtleBrush", ThemeVariant.Light, out var tint);

        Assert.Equal(Color.Parse("#0F766E"), Assert.IsType<ISolidColorBrush>(tint, exactMatch: false).Color);
    });

    [Fact]
    public Task Full_black_falls_back_to_dark_for_everything_it_does_not_change() => Ui.Run(() =>
    {
        Assert.Equal(ThemeVariant.Dark, ThemeService.Black.InheritVariant);
        Assert.True(Application.Current!.TryGetResource("SystemControlForegroundBaseHighBrush", ThemeService.Black, out _));
    });

    // Text on an accent fill must read on it: the presets are pastels, so they take black text.
    [Theory]
    [InlineData("#6DD3C4", "#FF000000")]
    [InlineData("#1E3A8A", "#FFFFFFFF")]
    public Task Text_on_the_accent_reads_on_it(string accent, string expected) => Ui.Run(() =>
    {
        new ThemeService().Apply(new AppSettings { Accent = accent });

        foreach (var key in ThemeService.OnAccentKeys)
        {
            Assert.True(Application.Current!.TryGetResource(key, ThemeVariant.Light, out var value), key);
            Assert.Equal(Color.Parse(expected), Assert.IsType<ISolidColorBrush>(value, exactMatch: false).Color);
        }
    });

    [Fact]
    public Task No_accent_keeps_fluents_own_text_on_accent() => Ui.Run(() =>
    {
        var service = new ThemeService();
        service.Apply(new AppSettings { Accent = "#6DD3C4" });
        service.Apply(new AppSettings { Accent = null });

        Assert.All(ThemeService.OnAccentKeys, key => Assert.False(Application.Current!.Resources.ContainsKey(key)));
    });

    // A pastel accent is too light for text on a light surface, and a deep one too dark on a dark
    // surface: Light uses the Dark2 shade, Dark the Light3 shade (WinUI's AccentTextFillColorPrimary).
    [Fact]
    public Task Accent_text_on_light_surfaces_uses_the_darker_shade() => Ui.Run(() =>
    {
        new ThemeService().Apply(new AppSettings { Accent = "#6DD3C4" });
        var shades = Copyloft.Core.Theming.AccentPalette.Shades("#6DD3C4");

        Application.Current!.TryGetResource("SystemControlForegroundAccentBrush", ThemeVariant.Light, out var light);
        Application.Current!.TryGetResource("SystemControlForegroundAccentBrush", ThemeVariant.Dark, out var dark);

        Assert.Equal(Color.Parse(shades.Dark2), Assert.IsType<ISolidColorBrush>(light, exactMatch: false).Color);
        Assert.Equal(Color.Parse(shades.Light3), Assert.IsType<ISolidColorBrush>(dark, exactMatch: false).Color);
    });
}
