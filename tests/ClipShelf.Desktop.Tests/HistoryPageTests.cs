using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using ClipShelf.Core.Models;
using ClipShelf.Desktop.Features;
using ClipShelf.Presentation.Features.ClipboardPanel;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

public sealed class HistoryPageTests
{
    private sealed record Hosted(ViewModelKit Kit, HistoryPage Page, Window Window)
    {
        public ListBox List => Page.FindControl<ListBox>("ClipList")!;

        public void Press(Key key)
        {
            Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static async Task<Hosted> Open(ViewModelKit kit)
    {
        var page = new HistoryPage(kit.ViewModel);
        var window = new Window { Content = page, Width = 800, Height = 600 };
        window.Show();
        await page.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        return new Hosted(kit, page, window);
    }

    private static ViewModelKit Clips(params string[] texts)
    {
        var kit = new ViewModelKit();
        foreach (var text in texts) kit.StoreClip(text);
        return kit;
    }

    [Fact]
    public Task Group_headers_cannot_be_selected_or_focused() => Ui.Run(async () =>
    {
        var hosted = await Open(Clips("one", "two"));

        var header = hosted.List.ContainerFromIndex(0)!;

        Assert.IsType<ClipGroup>(header.DataContext);
        Assert.False(header.IsEnabled);
        Assert.False(header.Focusable);
    });

    [Fact]
    public Task Enter_on_a_row_copies_it_and_says_so() => Ui.Run(async () =>
    {
        var hosted = await Open(Clips("one", "two"));
        hosted.List.SelectedIndex = 2;                       // [Recent], one, two
        hosted.List.ContainerFromIndex(2)!.Focus();

        hosted.Press(Key.Enter);

        hosted.Kit.Clipboard.Received().WriteText("two");
        var notice = hosted.Page.FindControl<TextBlock>("CopyNotice")!;
        Assert.True(notice.IsVisible);
        Assert.Equal("Copied", notice.Text);
    });

    [Fact]
    public Task The_footer_counts_every_stored_clip_not_just_the_loaded_ones() => Ui.Run(async () =>
    {
        var kit = Clips("one", "two");
        kit.StoredTotal = 532;

        var hosted = await Open(kit);

        Assert.Equal("532 clips", hosted.Page.FindControl<TextBlock>("FooterText")!.Text);
    });

    [Fact]
    public Task Escape_leaves_select_mode_then_cancels_the_confirm_and_never_more() => Ui.Run(async () =>
    {
        var hosted = await Open(Clips("one"));
        var vm = hosted.Kit.ViewModel;

        vm.ToggleSelectionCommand.Execute(null);
        hosted.Press(Key.Escape);
        Assert.False(vm.IsSelecting);

        vm.RequestClearCommand.Execute(null);
        hosted.Press(Key.Escape);
        Assert.False(vm.IsConfirmingClear);
    });

    [Theory]
    [InlineData(0, 500, 2000, false)]
    [InlineData(1460, 500, 2000, true)]          // within a row of the end
    [InlineData(1500, 500, 2000, true)]
    public void More_rows_load_near_the_end_of_the_scroll(double offset, double viewport, double extent, bool expected) =>
        Assert.Equal(expected, HistoryPage.IsNearEnd(offset, viewport, extent));

    [Fact]
    public Task A_locked_category_in_the_filter_falls_back_until_it_can_be_unlocked() => Ui.Run(async () =>
    {
        var kit = Clips("one");
        kit.Categories.GetCategoriesAsync(default).ReturnsForAnyArgs(new[] { new Category(5, "Work", IsLocked: true) });
        var hosted = await Open(kit);
        var filter = hosted.Page.FindControl<ComboBox>("CategoryFilter")!;

        filter.SelectedIndex = 1;                   // "Work (locked)"
        Dispatcher.UIThread.RunJobs();

        Assert.Null(kit.ViewModel.SelectedCategoryFilter!.Value);
    });
}
