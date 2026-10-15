using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Copyloft.Desktop.Features;

namespace Copyloft.Desktop.Tests;

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

    private static async Task<(HistoryPage Page, Copyloft.Desktop.Controls.ClipRow Row)> HistoryWithOneClip(bool rtl = false)
    {
        var kit = new ViewModelKit();
        kit.StoreClip("a clip long enough to run up to the row actions if nothing kept it back from them");
        var page = new HistoryPage(kit.ViewModel);
        new Window
        {
            Content = page, Width = 800, Height = 600,
            FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight,
        }.Show();
        await page.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        return (page, page.GetVisualDescendants().OfType<Copyloft.Desktop.Controls.ClipRow>().First());
    }

    private static Rect BoundsIn(Visual visual, Visual root) =>
        new(visual.TranslatePoint(new Point(0, 0), root)!.Value, visual.Bounds.Size);

    // Seen on Windows: the pin icon touched the clip text, and the scroll bar sat on the delete icon.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Row_actions_keep_clear_of_the_text_and_the_scroll_bar(bool rtl) => Ui.Run(async () =>
    {
        var (page, row) = await HistoryWithOneClip(rtl);
        var text = row.GetVisualDescendants().OfType<StackPanel>().First();
        var buttons = row.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("row")).ToList();
        var list = page.FindControl<ListBox>("ClipList")!;
        var t = BoundsIn(text, page); var first = BoundsIn(buttons[0], page); var last = BoundsIn(buttons[^1], page);
        var l = BoundsIn(list, page);

        // Either side, so the same check holds mirrored (TranslatePoint does not mirror RTL).
        var textGap = Math.Max(first.Left - t.Right, t.Left - first.Right);
        var edgeGap = Math.Min(l.Right - last.Right, last.Left - l.Left);
        Assert.True(textGap >= 12, $"text to pin gap {textGap}");
        Assert.True(edgeGap >= 12, $"delete to list edge {edgeGap}");
    });

    [Fact]
    public Task Row_actions_wear_the_accent_in_history_and_the_flyout() => Ui.Run(async () =>
    {
        var (_, row) = await HistoryWithOneClip();
        foreach (var icon in row.GetVisualDescendants().OfType<PathIcon>())
            Assert.Same(Resource("SystemControlForegroundAccentBrush", icon), icon.Foreground);

        var kit = new FlyoutTestKit();
        kit.AddClip("one");
        kit.Window.ShowFlyout();
        Dispatcher.UIThread.RunJobs();
        var flyoutRow = kit.Window.GetVisualDescendants().OfType<Copyloft.Desktop.Controls.ClipRow>().First();
        foreach (var icon in flyoutRow.GetVisualDescendants().OfType<PathIcon>().Where(i => i.IsVisible))
            Assert.Same(Resource("SystemControlForegroundAccentBrush", icon), icon.Foreground);
    });

    // Seen on Windows: the base accent (#0078D4) on a dark surface is too dark to read; WinUI uses
    // the accent's Light3 shade there (AccentTextFillColorPrimary).
    [Fact]
    public Task Accent_text_on_dark_uses_the_light_shade() => Ui.Run(() =>
    {
        Application.Current!.TryGetResource("SystemControlForegroundAccentBrush", Avalonia.Styling.ThemeVariant.Dark, out var brush);
        Application.Current!.TryGetResource("SystemAccentColorLight3", Avalonia.Styling.ThemeVariant.Dark, out var light3);
        Assert.Equal((Avalonia.Media.Color)light3!, Assert.IsType<Avalonia.Media.ISolidColorBrush>(brush, exactMatch: false).Color);
    });
}
