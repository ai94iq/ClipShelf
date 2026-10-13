using Avalonia;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Tests;

public sealed class FlyoutWindowTests
{
    [Fact]
    public Task Toggle_opens_then_closes() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();

        kit.Window.Toggle();
        Assert.True(kit.Window.IsOpen);

        kit.Window.Toggle();
        Assert.False(kit.Window.IsOpen);
    });

    [Fact]
    public Task Opening_resets_leftover_state_and_marks_the_view_visible() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.ViewModel.IsConfirmingClear = true;
        kit.ViewModel.IsViewVisible = false;

        kit.Window.ShowFlyout();

        Assert.False(kit.ViewModel.IsConfirmingClear);
        Assert.True(kit.ViewModel.IsViewVisible);
    });

    [Fact]
    public Task Closing_hands_the_foreground_back_to_the_previous_window() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.Platform.Foreground = new IntPtr(777);
        kit.Window.ShowFlyout();

        kit.Window.HideFlyout();

        Assert.Equal(new IntPtr(777), kit.Platform.RestoredTo);
        Assert.False(kit.ViewModel.IsViewVisible);
    });

    // A tray click first deactivates the open flyout (which hides it) and then toggles: that
    // toggle must not reopen it.
    [Fact]
    public Task A_toggle_right_after_a_deactivation_keeps_it_closed() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.Window.ShowFlyout();

        kit.Window.HideAfterDeactivation();
        kit.Time.Advance(TimeSpan.FromMilliseconds(100));
        kit.Window.Toggle();

        Assert.False(kit.Window.IsOpen);
    });

    [Fact]
    public Task A_later_toggle_opens_it_again() => Ui.Run(() =>
    {
        var kit = new FlyoutTestKit();
        kit.Window.ShowFlyout();
        kit.Window.HideAfterDeactivation();

        kit.Time.Advance(TimeSpan.FromSeconds(1));
        kit.Window.Toggle();

        Assert.True(kit.Window.IsOpen);
    });

    [Theory]
    [InlineData(100, 100, 1.0, 108, 108)]          // below-right of the pointer
    [InlineData(1900, 1060, 1.0, 1548, 588)]       // pushed back inside at the corner
    [InlineData(1900, 1060, 1.5, 1362, 342)]       // 150%: the flyout is 540x720 pixels
    public void The_flyout_sits_next_to_the_pointer_inside_the_work_area(
        int x, int y, double scaling, int expectedX, int expectedY)
    {
        var bounds = FlyoutWindow.Place(new PixelPoint(x, y), new PixelRect(0, 0, 1920, 1080), scaling);

        Assert.Equal(new PixelPoint(expectedX, expectedY), bounds.Position);
    }
}
