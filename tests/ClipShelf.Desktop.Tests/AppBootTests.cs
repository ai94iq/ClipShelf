using Avalonia;
using Avalonia.Themes.Fluent;

namespace ClipShelf.Desktop.Tests;

public sealed class AppBootTests
{
    [Fact]
    public Task The_app_runs_on_the_fluent_theme_only() => Ui.Run(() =>
        Assert.IsType<FluentTheme>(Assert.Single(Application.Current!.Styles)));
}
