using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Tests;

// Main's shell: content runs into the title bar, which carries the app name and (on Settings)
// the search; the caption buttons are ours, tagged so the OS treats them as caption buttons.
// Avalonia's own drawn caption doubled the title and added a fullscreen button.
public sealed class MainChromeTests
{
    private static MainWindow Shown()
    {
        var window = new MainWindow(new AppLifetime(), nativeCaption: false);
        window.ShowHistory();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static IEnumerable<Control> WithRole(Window window, WindowDecorationsElementRole role) =>
        window.GetVisualDescendants().OfType<Control>().Where(c => WindowDecorationProperties.GetElementRole(c) == role);

    [Fact]
    public Task The_content_runs_into_the_title_bar_with_only_a_border_drawn() => Ui.Run(() =>
    {
        var window = Shown();

        Assert.True(window.ExtendClientAreaToDecorationsHint);
        Assert.Equal(WindowDecorations.BorderOnly, window.WindowDecorations);
    });

    // macOS: the system's traffic lights, not Windows-style buttons (seen drawn on a Mac in a VM).
    [Fact]
    public Task On_a_mac_the_system_draws_the_window_buttons() => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime(), nativeCaption: true);
        window.ShowHistory();
        window.UpdateLayout();

        Assert.Equal(WindowDecorations.Full, window.WindowDecorations);
        Assert.True(window.ExtendClientAreaToDecorationsHint);
        Assert.False(window.FindControl<Control>("CaptionButtons")!.IsVisible);
        Assert.False(window.FindControl<Control>("AppTitle")!.IsVisible, "a mac title bar shows no app name or icon");
    });

    // Seen on a Mac: the Settings search sat right of center, pushed by the title.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task The_settings_search_is_centered_on_the_window(bool nativeCaption) => Ui.Run(() =>
    {
        var window = new MainWindow(new AppLifetime(), nativeCaption: nativeCaption);
        window.ShowSettings();
        window.UpdateLayout();

        var search = window.FindControl<TextBox>("SearchBox")!;
        var left = search.TranslatePoint(new Avalonia.Point(0, 0), window)!.Value.X;
        Assert.Equal(window.Bounds.Width / 2, left + search.Bounds.Width / 2, 1.0);
        Assert.Equal(360, search.Bounds.Width);
    });

    // Seen on Windows: the title icon came from app.ico's first frame (16 px) and was stretched
    // on a scaled display. It needs pixels for 300% and a smooth downscale.
    [Fact]
    public Task The_title_icon_stays_sharp_on_scaled_displays() => Ui.Run(() =>
    {
        var icon = Shown().FindControl<Image>("TitleIcon")!;

        var bitmap = Assert.IsAssignableFrom<Avalonia.Media.Imaging.Bitmap>(icon.Source);
        Assert.True(bitmap.PixelSize.Width >= 48, $"title icon is {bitmap.PixelSize.Width} px");
        Assert.Equal(Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality, RenderOptions.GetBitmapInterpolationMode(icon));
    });

    [Fact]
    public Task The_title_bar_names_the_app_once() => Ui.Run(() =>
    {
        var window = Shown();

        var bar = Assert.Single(WithRole(window, WindowDecorationsElementRole.TitleBar));
        Assert.Single(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Copyloft");
        Assert.Contains(bar.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Copyloft");
    });

    [Theory]
    [InlineData(WindowDecorationsElementRole.MinimizeButton)]
    [InlineData(WindowDecorationsElementRole.MaximizeButton)]
    [InlineData(WindowDecorationsElementRole.CloseButton)]
    public Task Each_caption_button_is_tagged_and_named(WindowDecorationsElementRole role) => Ui.Run(() =>
    {
        var button = Assert.IsType<Button>(Assert.Single(WithRole(Shown(), role)));

        Assert.False(string.IsNullOrEmpty(AutomationProperties.GetName(button)));
    });

    [Fact]
    public Task There_is_no_fullscreen_button() => Ui.Run(() =>
        Assert.Empty(WithRole(Shown(), WindowDecorationsElementRole.FullScreenButton)));

    [Fact]
    public Task The_settings_search_sits_in_the_title_bar() => Ui.Run(() =>
    {
        var window = Shown();
        window.ShowSettings();
        window.UpdateLayout();

        var bar = Assert.Single(WithRole(window, WindowDecorationsElementRole.TitleBar));
        var search = window.FindControl<TextBox>("SearchBox")!;
        Assert.True(search.IsVisible);
        Assert.Contains(search, bar.GetVisualDescendants());
        // The search is typed into, so it is not part of the drag area.
        Assert.Equal(WindowDecorationsElementRole.User, WindowDecorationProperties.GetElementRole(search));
    });

    [Fact]
    public Task Minimize_and_maximize_change_the_window_state() => Ui.Run(() =>
    {
        var window = Shown();
        var max = (Button)WithRole(window, WindowDecorationsElementRole.MaximizeButton).Single();

        max.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(WindowState.Maximized, window.WindowState);
        max.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(WindowState.Normal, window.WindowState);

        ((Button)WithRole(window, WindowDecorationsElementRole.MinimizeButton).Single()).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(WindowState.Minimized, window.WindowState);
    });

    [Fact]
    public Task Close_hides_the_window_like_the_system_close() => Ui.Run(() =>
    {
        var window = Shown();

        ((Button)WithRole(window, WindowDecorationsElementRole.CloseButton).Single()).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.False(window.IsVisible);
    });

    // Main's content sits on a layer with a rounded leading corner, under the title bar.
    [Fact]
    public Task The_content_sits_on_a_rounded_layer() => Ui.Run(() =>
    {
        var layer = Shown().FindControl<Border>("ContentLayer")!;

        Assert.Equal(8, layer.CornerRadius.TopLeft);
        Assert.NotNull(layer.Background);
    });
}
