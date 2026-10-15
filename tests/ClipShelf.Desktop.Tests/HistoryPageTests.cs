using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    // Seen on macOS: Select's label sat high and the disabled clear button was a short gray block.
    [Fact]
    public Task The_footer_buttons_line_up_and_an_empty_history_draws_no_gray_block() => Ui.Run(async () =>
    {
        var hosted = await Open(new ViewModelKit());
        hosted.Window.UpdateLayout();

        var clear = hosted.Page.FindControl<Button>("ClearAllButton")!;
        var select = hosted.Page.GetVisualDescendants().OfType<Button>()
            .Single(b => b.IsVisible && b != clear && b.FindAncestorOfType<Border>()?.Name == "Footer");
        Assert.False(clear.IsEnabled);
        Assert.Equal(select.Bounds.Height, clear.Bounds.Height);
        var fill = clear.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First().Background;
        Assert.True(fill is null || fill is Avalonia.Media.ISolidColorBrush { Color.A: 0 }, "disabled clear has no fill");
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
    public Task A_locked_category_in_the_filter_asks_for_the_password() => Ui.Run(async () =>
    {
        var kit = Clips("one");
        kit.StoredCategories.Add(new Category(5, "Work", IsLocked: true));
        kit.Passwords.Verify("1234").Returns(true);
        var hosted = await Open(kit);

        hosted.Page.FindControl<ComboBox>("CategoryFilter")!.SelectedIndex = 1;   // "Work (locked)"
        var dialog = DialogDriver.Open(hosted.Window);
        DialogDriver.Boxes(dialog)[0].Text = "1234";
        DialogDriver.Click(dialog.FindControl<Button>("PrimaryButton")!);

        kit.Locks.Received().Unlock(5);
        Assert.Equal(5, kit.ViewModel.SelectedCategoryFilter!.Value);
    });

    [Fact]
    public Task Cancelling_the_password_falls_back_to_the_previous_filter() => Ui.Run(async () =>
    {
        var kit = Clips("one");
        kit.StoredCategories.Add(new Category(5, "Work", IsLocked: true));
        var hosted = await Open(kit);

        hosted.Page.FindControl<ComboBox>("CategoryFilter")!.SelectedIndex = 1;
        DialogDriver.Click(DialogDriver.Open(hosted.Window).FindControl<Button>("CancelButton")!);

        Assert.Null(kit.ViewModel.SelectedCategoryFilter!.Value);
        kit.Locks.DidNotReceive().Unlock(Arg.Any<long>());
    });

    [Fact]
    public Task Keys_belong_to_an_open_dialog() => Ui.Run(async () =>
    {
        var kit = Clips("one");
        var hosted = await Open(kit);
        kit.ViewModel.ToggleSelectionCommand.Execute(null);
        _ = new ClipShelf.Desktop.Controls.InlineDialog("t", "ok").ShowAsync(hosted.Window);
        Dispatcher.UIThread.RunJobs();

        hosted.Press(Key.Escape);

        Assert.True(kit.ViewModel.IsSelecting);    // Esc closed the dialog, not select mode
        Assert.Null(ClipShelf.Desktop.Controls.InlineDialog.OpenIn(hosted.Window));
    });
}
