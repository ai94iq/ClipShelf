using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Tests;

public sealed class MainWindowTests
{
    [Fact]
    public Task Show_history_opens_on_history_without_the_search_box() => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime());

        window.ShowHistory();

        Assert.True(window.IsVisible);
        Assert.False(window.IsSettings);
        Assert.False(window.IsSearchVisible);
    });

    [Fact]
    public Task Show_settings_opens_on_settings_with_the_search_box() => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime());

        window.ShowSettings();

        Assert.True(window.IsSettings);
        Assert.True(window.IsSearchVisible);
    });

    [Fact]
    public Task Picking_settings_in_the_rail_switches_the_section() => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime());
        window.ShowHistory();

        window.FindControl<RadioButton>("SettingsButton")!.IsChecked = true;

        Assert.True(window.IsSettings);
    });

    [Fact]
    public Task Leaving_settings_clears_the_search() => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime());
        window.ShowSettings();
        window.SearchText = "tray";

        window.ShowHistory();

        Assert.Equal(string.Empty, window.SearchText);
    });

    [Fact]
    public Task Closing_hides_the_window_while_the_app_keeps_running() => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime());
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.ShowHistory();

        window.Close();

        Assert.False(window.IsVisible);
        Assert.False(closed);
    });

    [Fact]
    public Task Closing_while_exiting_really_closes() => Ui.Run(() =>
    {
        var lifetime = new AppLifetime();
        var window = new MainWindow(lifetime);
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.ShowHistory();

        lifetime.IsExiting = true;
        window.Close();

        Assert.True(closed);
    });

    [Theory]
    [InlineData("HistoryButton", "History")]
    [InlineData("SettingsButton", "Settings")]
    public Task Rail_buttons_have_screen_reader_names(string button, string name) => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime());

        Assert.Equal(name, AutomationProperties.GetName(window.FindControl<RadioButton>(button)!));
    });
}
