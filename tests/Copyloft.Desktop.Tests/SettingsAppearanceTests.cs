using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Copyloft.Desktop.Controls;
using Copyloft.Presentation.Hosting;

namespace Copyloft.Desktop.Tests;

public sealed class SettingsAppearanceTests
{
    private static Window Host(Control page)
    {
        var window = new Window { Content = page, Width = 900, Height = 700 };
        window.Show();
        return window;
    }

    private static object? Convert(string? hex, string? parameter = null) =>
        AccentSwatchConverter.Instance.Convert(hex, typeof(IBrush), parameter, CultureInfo.InvariantCulture);

    [Fact]
    public Task A_preset_swatch_shows_its_color() => Ui.Run(() =>
        Assert.Equal(Color.Parse("#0F766E"), Assert.IsType<SolidColorBrush>(Convert("#0F766E")).Color));

    [Fact]
    public Task The_system_swatch_shows_the_platform_accent() => Ui.Run(() =>
        Assert.IsType<SolidColorBrush>(Convert(null)));

    [Fact]
    public Task Every_accent_is_a_named_swatch() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        var swatches = kit.Page.GetVisualDescendants().OfType<RadioButton>().Where(r => r.Classes.Contains("swatch")).ToList();

        Assert.Equal(kit.ViewModel.AccentOptions.Count, swatches.Count);
        Assert.All(swatches, s => Assert.False(string.IsNullOrEmpty(Avalonia.Automation.AutomationProperties.GetName(s))));
    });

    [Fact]
    public Task Picking_a_swatch_selects_that_accent_and_offers_a_restart() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new AppSettings { Accent = null });
        Host(kit.Page);
        var restartBar = kit.Page.FindControl<Control>("RestartBar")!;
        Assert.False(restartBar.IsVisible);

        var swatch = kit.Page.GetVisualDescendants().OfType<RadioButton>().Where(r => r.Classes.Contains("swatch")).ElementAt(2);
        swatch.IsChecked = true;

        Assert.Same(kit.ViewModel.AccentOptions[2], kit.ViewModel.SelectedAccent);
        Assert.Equal(kit.ViewModel.AccentOptions[2].Value, kit.Settings.Current.Accent);
        Assert.True(restartBar.IsVisible);
    });

    [Fact]
    public Task Restart_now_asks_the_shell_to_restart() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        var restarted = false;
        kit.Shell.RestartRequested += (_, _) => restarted = true;
        Host(kit.Page);

        kit.Page.FindControl<Button>("RestartButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.True(restarted);
    });
}
