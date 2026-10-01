using ClipShelf.App.Platform;

namespace ClipShelf.App.Tests;

public sealed class PanelDismissTests
{
    private static readonly IntPtr Flyout = new(1);
    private static readonly IntPtr Other = new(2);

    [Fact]
    public void A_foreign_window_taking_foreground_hides_the_flyout() =>
        Assert.True(PanelDismiss.ShouldHide(Flyout, Other, "Notepad", isVisible: true));

    [Fact]
    public void Another_window_of_this_app_hides_the_flyout() =>
        Assert.True(PanelDismiss.ShouldHide(Flyout, Other, "WinUIDesktopWin32WindowClass", isVisible: true));

    [Fact]
    public void Tray_clicks_leave_the_flyout_to_its_own_toggle() =>
        Assert.False(PanelDismiss.ShouldHide(Flyout, Other, "Shell_TrayWnd", isVisible: true));

    [Fact]
    public void The_flyout_itself_never_hides_itself() =>
        Assert.False(PanelDismiss.ShouldHide(Flyout, Flyout, "WinUIDesktopWin32WindowClass", isVisible: true));

    [Fact]
    public void A_hidden_flyout_ignores_foreground_changes() =>
        Assert.False(PanelDismiss.ShouldHide(Flyout, Other, "Notepad", isVisible: false));

    [Fact]
    public void A_missing_foreground_window_ignores_the_event() =>
        Assert.False(PanelDismiss.ShouldHide(Flyout, IntPtr.Zero, null, isVisible: true));
}
