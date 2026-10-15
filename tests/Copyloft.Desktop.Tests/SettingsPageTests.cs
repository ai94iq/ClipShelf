using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Copyloft.Core.Models;
using Copyloft.Presentation.Hosting;
using Copyloft.Desktop.Features;
using Copyloft.Desktop.Shell;
using NSubstitute;

namespace Copyloft.Desktop.Tests;

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

    // On Wayland the hotkey only works while a Copyloft window has focus; the note says how to
    // bind a system shortcut instead. Elsewhere it stays out of the way.
    [Fact]
    public Task The_wayland_hotkey_note_shows_only_in_a_wayland_session() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        var note = kit.Page.FindControl<Control>("WaylandHotkeyNote")!;
        Assert.Equal(Copyloft.Desktop.Platform.DesktopSession.IsWayland, note.IsVisible);
        Assert.Contains("copyloft --toggle", Copyloft.Presentation.Localization.Tr.Get("Settings_HotkeyWayland"));
    });

    // Seen on macOS: a Mica setting and "Ctrl, Alt, Shift or Win". Each OS's CI checks its own case.
    [Fact]
    public Task Settings_offer_only_what_this_os_has() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        Assert.Equal(OperatingSystem.IsWindows(), kit.Page.FindControl<Control>("BackdropCard")!.IsVisible);
        var hint = kit.Page.FindControl<Copyloft.Desktop.Controls.SettingsCard>("HotkeyCard")!.Description!;
        Assert.Contains(OperatingSystem.IsMacOS() ? "Cmd" : "Win", hint);
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
        var kit = new SettingsKit(new AppSettings { Backdrop = Copyloft.Core.Theming.BackdropKind.Mica });
        Host(kit.Page);

        kit.Page.FindControl<ComboBox>("BackdropCombo")!.SelectedItem =
            kit.ViewModel.BackdropOptions.First(o => o.Value == Copyloft.Core.Theming.BackdropKind.Acrylic);

        kit.Theme.Received().Apply(Arg.Is<AppSettings>(s => s.Backdrop == Copyloft.Core.Theming.BackdropKind.Acrylic));
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

    // Seen on Windows: Set password, Remove, Check for updates, Export and the category actions
    // were taller than their rows with the label off center; Export stood taller than its combo.
    [Fact]
    public Task Settings_buttons_fit_their_rows() => Ui.Run(async () =>
    {
        var kit = new SettingsKit(passwordSet: true);
        kit.StoredCategories.Add(new Category(1, "Work"));
        var window = Host(kit.Page);
        await kit.Page.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var named = new[] { "PasswordButton", "RemovePasswordButton", "CheckUpdatesButton", "ExportButton" }
            .Select(n => kit.Page.FindControl<Button>(n)!);
        var rename = kit.Page.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("rename"));
        foreach (var button in named.Append(rename))
        {
            Assert.Equal(32, button.Bounds.Height);
            Assert.Equal(Avalonia.Layout.VerticalAlignment.Center, button.VerticalContentAlignment);
        }
        var export = kit.Page.FindControl<Button>("ExportButton")!;
        var format = ((Panel)export.Parent!).Children.OfType<ComboBox>().Single();
        Assert.Equal(export.Bounds.Height, format.Bounds.Height);
    });

    [Fact]
    public Task A_successful_export_says_so_in_green() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        kit.ViewModel.ExportStatus = "Exported 3 clips";
        Dispatcher.UIThread.RunJobs();

        var status = kit.Page.FindControl<TextBlock>("ExportStatus")!;
        Assert.True(status.IsVisible);
        Avalonia.Application.Current!.TryGetResource("CopyloftSuccessBrush", status.ActualThemeVariant, out var green);
        Assert.NotNull(green);
        Assert.Same(green, status.Foreground);
    });
}
