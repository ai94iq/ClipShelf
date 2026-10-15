using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Copyloft.Desktop.Shell;

// A small note where the flyout was, for sessions where the pick cannot be pasted for the user:
// "Copied, press Ctrl+V". It never takes focus, so Ctrl+V lands in the app the user is in.
public sealed class PasteHint : Window
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(2.5);
    private readonly DispatcherTimer _timer;

    public PasteHint()
    {
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        Topmost = true;
        ShowActivated = false;
        ShowInTaskbar = false;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        // The edge keeps the note readable over a window of the same color (seen on macOS over a
        // white Terminal).
        var frame = new Border
        {
            Padding = new Thickness(16, 10),
            BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = Text, VerticalAlignment = VerticalAlignment.Center },
        };
        frame[!Border.BorderBrushProperty] = frame.GetResourceObservable("SystemControlTransientBorderBrush").ToBinding();
        Content = frame;
        this[!BackgroundProperty] = this.GetResourceObservable("CopyloftCardBrush").ToBinding();
        _timer = new DispatcherTimer { Interval = Duration };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Hide();
        };
    }

    public string Text { get; } = Tr.Get(OperatingSystem.IsMacOS() ? "Flyout_PasteHintMac" : "Flyout_PasteHint");

    public void ShowAt(PixelPoint position)
    {
        Position = position;
        Show();
        _timer.Stop();
        _timer.Start();
    }
}
