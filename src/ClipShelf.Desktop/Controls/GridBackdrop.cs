using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ClipShelf.Desktop.Controls;

// The construction grid: hairline rules at a fixed pitch, drawn in one pass. Cheap by design:
// no bitmaps, no animation, and hit-test transparent so it never blocks the content above it.
public sealed class GridBackdrop : Control
{
    public static readonly StyledProperty<double> PitchProperty =
        AvaloniaProperty.Register<GridBackdrop, double>(nameof(Pitch), 24d);

    static GridBackdrop() => AffectsRender<GridBackdrop>(PitchProperty);

    public double Pitch
    {
        get => GetValue(PitchProperty);
        set => SetValue(PitchProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var pitch = Pitch;
        if (pitch < 4) return;

        var line = this.FindResource("GridLineBrush") as IBrush ?? Brushes.Gray;
        var pen = new Pen(line, 1);
        var width = Bounds.Width;
        var height = Bounds.Height;

        for (var x = pitch; x < width; x += pitch)
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));

        for (var y = pitch; y < height; y += pitch)
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
    }
}
