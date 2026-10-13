using Avalonia.Controls;
using Avalonia.Threading;
using ClipShelf.Core.Models;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Desktop.Features;
using ClipShelf.Desktop.Shell;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

public sealed class SettingsPageTests
{
    private static readonly string[] Groups =
        ["AppearanceGroup", "TrayGroup", "BehaviorGroup", "HistoryGroup", "ExportGroup", "CategoriesGroup", "UpdatesGroup", "ExitGroup"];

    private static Window Host(SettingsPage page)
    {
        var window = new Window { Content = page, Width = 900, Height = 700 };
        window.Show();
        return window;
    }

    [Fact]
    public Task Every_group_shows_without_a_filter() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        foreach (var group in Groups)
            Assert.True(kit.Page.FindControl<Control>(group)!.IsVisible, group);
        Assert.False(kit.Page.FindControl<Control>("NoMatches")!.IsVisible);
    });

    [Fact]
    public Task A_filter_keeps_only_the_matching_groups() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        kit.ViewModel.Filter = "tray";

        Assert.True(kit.Page.FindControl<Control>("TrayGroup")!.IsVisible);
        Assert.False(kit.Page.FindControl<Control>("AppearanceGroup")!.IsVisible);
    });

    [Fact]
    public Task A_filter_with_no_match_says_so() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        kit.ViewModel.Filter = "zzzz";

        Assert.True(kit.Page.FindControl<Control>("NoMatches")!.IsVisible);
        Assert.All(Groups, group => Assert.False(kit.Page.FindControl<Control>(group)!.IsVisible));
    });

    [Fact]
    public Task Turning_a_switch_saves_the_setting() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new AppSettings { ClearOnSignOut = false });
        Host(kit.Page);

        kit.Page.FindControl<ToggleSwitch>("ClearOnSignOutSwitch")!.IsChecked = true;

        Assert.True(kit.Settings.Current.ClearOnSignOut);
    });

    [Fact]
    public Task The_tray_double_click_switch_saves() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new AppSettings { OpenHistoryOnDoubleClick = true });
        Host(kit.Page);

        kit.Page.FindControl<ToggleSwitch>("DoubleClickSwitch")!.IsChecked = false;

        Assert.False(kit.Settings.Current.OpenHistoryOnDoubleClick);
    });

    [Fact]
    public Task Picking_a_theme_applies_it_at_once() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new AppSettings { Theme = AppTheme.Light });
        Host(kit.Page);

        var combo = kit.Page.FindControl<ComboBox>("ThemeCombo")!;
        combo.SelectedItem = kit.ViewModel.ThemeOptions.First(o => o.Value == AppTheme.Dark);

        kit.Theme.Received().Apply(Arg.Is<AppSettings>(s => s.Theme == AppTheme.Dark));
    });

    [Fact]
    public Task Picking_a_backdrop_applies_it_at_once() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new AppSettings { Backdrop = ClipShelf.Core.Theming.BackdropKind.Mica });
        Host(kit.Page);

        kit.Page.FindControl<ComboBox>("BackdropCombo")!.SelectedItem =
            kit.ViewModel.BackdropOptions.First(o => o.Value == ClipShelf.Core.Theming.BackdropKind.Acrylic);

        kit.Theme.Received().Apply(Arg.Is<AppSettings>(s => s.Backdrop == ClipShelf.Core.Theming.BackdropKind.Acrylic));
    });

    [Fact]
    public Task The_history_size_box_saves_a_number() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new AppSettings { MaxItems = 100 });
        Host(kit.Page);

        kit.Page.FindControl<NumericUpDown>("MaxItemsBox")!.Value = 250;

        Assert.Equal(250, kit.Settings.Current.MaxItems);
    });

    [Fact]
    public Task Loading_lists_the_categories() => Ui.Run(async () =>
    {
        var kit = new SettingsKit();
        kit.StoredCategories.Add(new Category(1, "Work"));
        Host(kit.Page);

        await kit.Page.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(kit.ViewModel.CategoryRows);
        Assert.False(kit.Page.FindControl<Control>("CategoriesEmpty")!.IsVisible);
    });
}
