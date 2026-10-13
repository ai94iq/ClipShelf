using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Features;

namespace ClipShelf.Desktop.Tests;

// Main's flyout and lists: a framed, rounded flyout with a quiet search line, rows that answer
// the pointer, and group headers ruled off from their rows.
public sealed class RowAndFlyoutLookTests
{
    private static object? Resource(string key, Control scope)
    {
        Application.Current!.TryGetResource(key, scope.ActualThemeVariant, out var value);
        return value;
    }

    [Fact]
    public Task The_flyout_keeps_the_platform_frame() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();

        // BorderOnly gives Windows 11's rounded corners and shadow; None is a bare rectangle.
        Assert.Equal(WindowDecorations.BorderOnly, kit.Window.WindowDecorations);
        Assert.DoesNotContain(kit.Window.GetVisualDescendants().OfType<Border>(),
            b => b.BorderThickness == new Thickness(1) && b.Child is Grid { Name: "Root" });
    });

    [Fact]
    public Task The_flyout_search_is_a_quiet_line() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.Window.ShowFlyout();
        Dispatcher.UIThread.RunJobs();

        var search = kit.Window.FindControl<TextBox>("SearchBox")!;

        Assert.Equal(new Thickness(0), search.BorderThickness);
    });

    [Fact]
    public Task A_row_answers_the_pointer() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.AddClip("one");
        kit.Window.ShowFlyout();
        Dispatcher.UIThread.RunJobs();
        var row = kit.Window.FindControl<ListBox>("ClipList")!.GetVisualDescendants().OfType<ListBoxItem>().First();
        row.IsSelected = false;     // the keyboard row's accent tint would win over hover

        ((IPseudoClasses)row.Classes).Set(":pointerover", true);
        Dispatcher.UIThread.RunJobs();

        var presenter = row.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
        Assert.Same(Resource("SystemControlHighlightListLowBrush", row), presenter.Background);
    });

    [Fact]
    public Task History_group_headers_are_ruled_off() => Ui.Run(async () =>
    {
        var kit = new ViewModelKit();
        kit.StoreClip("one");
        var page = new HistoryPage(kit.ViewModel);
        new Window { Content = page, Width = 800, Height = 600 }.Show();
        await page.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        var header = page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Recent");
        var rule = ((Visual)header.Parent!).GetVisualDescendants().OfType<Border>().SingleOrDefault(b => b.Classes.Contains("rule"));

        Assert.NotNull(rule);
    });
}
