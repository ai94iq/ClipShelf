using ClipShelf.App.Platform;

namespace ClipShelf.App.Tests;

public sealed class PanelPlacementTests
{
    private const int Width = 360;
    private const int Height = 480;
    private const int Margin = 12;
    private const int Gap = 8;

    [Fact]
    public void The_flyout_sits_below_right_of_the_pointer()
    {
        var (x, y) = Place(pointerX: 600, pointerY: 400, workRight: 1920, workBottom: 1040);

        Assert.Equal(600 + Gap, x);
        Assert.Equal(400 + Gap, y);
    }

    [Fact]
    public void The_flyout_tucks_back_inside_near_the_bottom_right_corner()
    {
        var (x, y) = Place(pointerX: 1900, pointerY: 1030, workRight: 1920, workBottom: 1040);

        Assert.Equal(1920 - Margin - Width, x);
        Assert.Equal(1040 - Margin - Height, y);
    }

    [Fact]
    public void A_click_on_the_tray_icon_still_lands_in_the_corner()
    {
        // The taskbar is outside the work area, so the pointer is below it.
        var (x, y) = Place(pointerX: 1800, pointerY: 1065, workRight: 1920, workBottom: 1040);

        Assert.Equal(1920 - Margin - Width, x);
        Assert.Equal(1040 - Margin - Height, y);
    }

    [Fact]
    public void The_flyout_stays_inside_the_work_area_on_a_second_monitor()
    {
        var (x, y) = Place(pointerX: -1918, pointerY: 200, workLeft: -1920, workTop: 0, workRight: 0, workBottom: 1040);

        Assert.Equal(-1920 + Margin, x);
        Assert.Equal(200 + Gap, y);
    }

    private static (int X, int Y) Place(
        int pointerX,
        int pointerY,
        int workLeft = 0,
        int workTop = 0,
        int workRight = 1920,
        int workBottom = 1040) =>
        PanelPlacement.NearCursor(
            pointerX, pointerY, Width, Height, workLeft, workTop, workRight, workBottom, Margin, Gap);
}
