using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using ClipShelf.Presentation.Common;

namespace ClipShelf.Desktop.Tests;

public sealed class FlyoutChromeTests
{
    private static FlyoutTestKit Open(params string[] clips)
    {
        var kit = new FlyoutTestKit();
        foreach (var clip in clips) kit.AddClip(clip);
        kit.Window.ShowFlyout();
        Dispatcher.UIThread.RunJobs();
        return kit;
    }

    private static T Find<T>(FlyoutTestKit kit, string name) where T : Control =>
        kit.Window.FindControl<T>(name) ?? throw new InvalidOperationException($"{name} not found");

    [Theory]
    [InlineData("OpenHistoryButton", "Open full history")]
    [InlineData("SelectButton", "Select")]
    [InlineData("ClearAllButton", "Clear all")]
    [InlineData("CancelSelectButton", "Cancel")]
    [InlineData("CopySelectionButton", "Copy")]
    public Task Footer_buttons_have_screen_reader_names(string button, string name) => Ui.Run(() =>
    {
        var kit = Open("alpha");

        Assert.Equal(name, AutomationProperties.GetName(Find<Button>(kit, button)));
    });

    [Fact]
    public Task Select_mode_swaps_the_footer_buttons() => Ui.Run(() =>
    {
        var kit = Open("alpha");

        kit.ViewModel.ToggleSelectionCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(Find<Button>(kit, "SelectButton").IsVisible);
        Assert.True(Find<Button>(kit, "CancelSelectButton").IsVisible);
        Assert.True(Find<Button>(kit, "CopySelectionButton").IsVisible);
    });

    [Fact]
    public Task The_clear_confirm_covers_the_flyout_while_confirming() => Ui.Run(() =>
    {
        var kit = Open("alpha");
        Assert.False(Find<Control>(kit, "ConfirmPanel").IsVisible);

        kit.ViewModel.RequestClearCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Find<Control>(kit, "ConfirmPanel").IsVisible);
    });

    [Fact]
    public Task A_deleted_clip_offers_undo() => Ui.Run(async () =>
    {
        var kit = Open("alpha");

        await kit.ViewModel.DeleteAsync(kit.ViewModel.Items[0]);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Find<Control>(kit, "UndoBar").IsVisible);
    });

    [Fact]
    public Task An_empty_history_says_so() => Ui.Run(() =>
    {
        var kit = Open();
        kit.ViewModel.State = LoadState.Empty;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Find<Control>(kit, "EmptyState").IsVisible);
        Assert.Equal(kit.ViewModel.EmptyTitle, Find<TextBlock>(kit, "EmptyTitle").Text);
    });

    [Fact]
    public Task An_error_offers_retry() => Ui.Run(() =>
    {
        var kit = Open("alpha");
        kit.ViewModel.State = LoadState.Error;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Find<Control>(kit, "ErrorState").IsVisible);
    });
}
