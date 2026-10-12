using Avalonia;
using Avalonia.Media;
using ClipShelf.Desktop.Services;
using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Desktop.Tests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData(AppTheme.System, "Default")]
    [InlineData(AppTheme.Light, "Light")]
    [InlineData(AppTheme.White, "Light")]
    [InlineData(AppTheme.Dark, "Dark")]
    [InlineData(AppTheme.Black, "Dark")]
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
}
