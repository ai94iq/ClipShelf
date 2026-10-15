using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Controls;
using ClipShelf.Desktop.Features;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Tests;

// What the owner sees, measured: sizes and weights that match main's WinUI screens. These catch
// styles that silently stop applying, which behavior tests never notice.
public sealed class VisualGuardTests
{
    private static T Laid<T>(T window) where T : Window
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    [Fact]
    public Task Row_action_icons_are_16_px() => Ui.Run(async () =>
    {
        var kit = new ViewModelKit();
        kit.StoreClip("one");
        var page = new HistoryPage(kit.ViewModel);
        Laid(new Window { Content = page, Width = 800, Height = 600 });
        await page.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        page.UpdateLayout();

        var icons = page.GetVisualDescendants().OfType<AppIcon>()
            .Where(i => i.FindAncestorOfType<ClipRow>() is not null && i.IsEffectivelyVisible).ToList();

        Assert.NotEmpty(icons);
        Assert.All(icons, i => Assert.Equal((16, 16), (i.Bounds.Width, i.Bounds.Height)));
    });

    [Fact]
    public Task Flyout_footer_icons_are_16_px() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.Window.ShowFlyout();
        Dispatcher.UIThread.RunJobs();
        kit.Window.UpdateLayout();

        var icon = kit.Window.FindControl<Button>("OpenHistoryButton")!.GetVisualDescendants().OfType<AppIcon>().Single();

        Assert.Equal((16, 16), (icon.Bounds.Width, icon.Bounds.Height));
        Avalonia.Application.Current!.TryGetResource("SystemControlForegroundAccentBrush", kit.Window.ActualThemeVariant, out var accent);
        Assert.Same(accent, icon.Foreground);     // action glyphs wear the accent, as the row actions do
    });

    [Fact]
    public Task Rail_icons_are_20_px() => Ui.Run(() =>
    {
        var window = Laid(new MainWindow(new AppLifetime()));

        var icon = window.FindControl<RadioButton>("HistoryButton")!.GetVisualDescendants().OfType<AppIcon>().Single();

        Assert.Equal((20, 20), (icon.Bounds.Width, icon.Bounds.Height));
    });

    [Fact]
    public Task The_selected_swatch_shows_its_check() => Ui.Run(() =>
    {
        var kit = new SettingsKit(new ClipShelf.Presentation.Hosting.AppSettings { Accent = "#6DD3C4" });
        Laid(new Window { Content = kit.Page, Width = 900, Height = 700 });

        var selected = kit.Page.GetVisualDescendants().OfType<RadioButton>().Single(r => r.Classes.Contains("swatch") && r.IsChecked == true);
        var check = selected.GetVisualDescendants().OfType<AppIcon>().Single(i => i.Name == "Check");

        Assert.Equal(1, check.Opacity);
    });

    // Main's type ramp: page titles 28 semibold (Title), group titles 14 semibold (BodyStrong).
    [Fact]
    public Task Settings_titles_follow_mains_type_ramp() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Laid(new Window { Content = kit.Page, Width = 900, Height = 700 });

        var title = kit.Page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Settings");
        var group = kit.Page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("group"));

        Assert.Equal((28.0, Avalonia.Media.FontWeight.SemiBold), (title.FontSize, title.FontWeight));
        Assert.Equal((14.0, Avalonia.Media.FontWeight.SemiBold), (group.FontSize, group.FontWeight));
    });

    [Fact]
    public Task The_history_title_matches_settings() => Ui.Run(() =>
    {
        var page = new HistoryPage(new ViewModelKit().ViewModel);
        Laid(new Window { Content = page, Width = 800, Height = 600 });

        var title = page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "History");

        Assert.Equal(28.0, title.FontSize);
    });
}
