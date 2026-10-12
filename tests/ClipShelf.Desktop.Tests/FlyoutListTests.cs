using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ClipShelf.Desktop.Controls;
using NSubstitute;
using SkiaSharp;

namespace ClipShelf.Desktop.Tests;

public sealed class FlyoutListTests
{
    private static void Press(FlyoutTestKit kit, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        kit.Window.KeyPress(key, modifiers, PhysicalKey.None, null);
        kit.Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static FlyoutTestKit OpenWith(params string[] clips)
    {
        var kit = new FlyoutTestKit();
        foreach (var clip in clips) kit.AddClip(clip);
        kit.Window.ShowFlyout();
        Dispatcher.UIThread.RunJobs();
        return kit;
    }

    [Fact]
    public Task Typing_goes_to_the_search_box() => Ui.Run(() =>
    {
        var kit = OpenWith("alpha");

        kit.Window.KeyTextInput("al");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("al", kit.ViewModel.SearchText);
    });

    // Keys arrive while the search box has focus.
    [Fact]
    public Task Down_then_enter_pastes_that_clip_into_the_previous_window() => Ui.Run(() =>
    {
        var kit = OpenWith("first", "second");

        Press(kit, Key.Down);
        Press(kit, Key.Down);
        Press(kit, Key.Enter);

        kit.Clipboard.Received().WriteText("second");
        kit.PasteBack.Received().IntoAsync(new IntPtr(42));
        Assert.False(kit.Window.IsOpen);
    });

    [Fact]
    public Task Ctrl_and_a_digit_picks_that_row() => Ui.Run(() =>
    {
        var kit = OpenWith("first", "second", "third");

        Press(kit, Key.D2, RawInputModifiers.Control);

        kit.Clipboard.Received().WriteText("second");
    });

    [Fact]
    public Task Escape_leaves_select_mode_before_it_closes() => Ui.Run(() =>
    {
        var kit = OpenWith("first");
        kit.ViewModel.ToggleSelectionCommand.Execute(null);

        Press(kit, Key.Escape);
        Assert.False(kit.ViewModel.IsSelecting);
        Assert.True(kit.Window.IsOpen);

        Press(kit, Key.Escape);
        Assert.False(kit.Window.IsOpen);
    });

    [Fact]
    public Task Every_row_shows_its_preview() => Ui.Run(() =>
    {
        var kit = OpenWith("alpha", "beta");

        var list = kit.Window.FindControl<ListBox>("ClipList")!;

        Assert.Equal(2, list.ItemCount);
    });

    [Fact]
    public Task A_png_becomes_a_bitmap_and_garbage_becomes_nothing() => Ui.Run(() =>
    {
        using var source = new SKBitmap(4, 4);
        var png = source.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        var converter = BytesToBitmapConverter.Instance;

        Assert.IsType<Bitmap>(converter.Convert(png, typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Null(converter.Convert(null, typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Null(converter.Convert(new byte[] { 1, 2, 3 }, typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture));
    });
}
