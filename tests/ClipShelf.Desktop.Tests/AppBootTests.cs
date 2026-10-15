using Avalonia;
using Avalonia.Themes.Fluent;

namespace ClipShelf.Desktop.Tests;

public sealed class AppBootTests
{
    [Fact]
    public Task The_app_runs_on_the_fluent_theme_only() => Ui.Run(() =>
        Assert.IsType<FluentTheme>(Assert.Single(Application.Current!.Styles)));

    // macOS shows this name in system prompts (the Keychain asked on behalf of "Avalonia Application").
    [Fact]
    public Task The_app_is_named_for_the_os() => Ui.Run(() =>
        Assert.Equal("Copyloft", Application.Current!.Name));
}
