using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClipShelf.Desktop.Resources;

namespace ClipShelf.Desktop.Controls;

// Outline icon drawn from Resources/Icons.g.cs. Usage: <controls:AppIcon Kind="Search" />.
// Takes the surrounding text color. Not mirrored in RTL unless IsDirectional="True"
// (use that for arrows, chevrons, back/forward, send). Default size 20.
public sealed class AppIcon : Control
{
    public static readonly StyledProperty<IconKind> KindProperty =
        AvaloniaProperty.Register<AppIcon, IconKind>(nameof(Kind));

    public static readonly StyledProperty<bool> IsDirectionalProperty =
        AvaloniaProperty.Register<AppIcon, bool>(nameof(IsDirectional));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<AppIcon, IBrush?>(nameof(Foreground));

    private StreamGeometry? _geometry;

    static AppIcon()
    {
        AffectsRender<AppIcon>(KindProperty, ForegroundProperty);
        KindProperty.Changed.AddClassHandler<AppIcon>((c, _) => c.UpdateGeometry());
    }

    public AppIcon()
    {
        Width = 20;
        Height = 20;
        Focusable = false;
        UpdateFlow();
        UpdateGeometry();
    }

    public IconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public bool IsDirectional
    {
        get => GetValue(IsDirectionalProperty);
        set => SetValue(IsDirectionalProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDirectionalProperty)
            UpdateFlow();
    }

    // Avalonia geometries can be re-drawn, but each icon still keeps its own parsed copy.
    private void UpdateGeometry()
    {
        var data = IconData.Get(Kind);
        _geometry = string.IsNullOrEmpty(data) ? null : StreamGeometry.Parse(data);
        InvalidateVisual();
    }

    private void UpdateFlow()
    {
        if (IsDirectional) ClearValue(FlowDirectionProperty);   // inherit, so it mirrors in RTL
        else FlowDirection = FlowDirection.LeftToRight;
    }

    public override void Render(DrawingContext context)
    {
        if (_geometry is null)
            return;

        var fill = Foreground;
        if (fill is null && this.TryFindResource("GridMutedBrush", out var resource) && resource is IBrush brush)
            fill ??= brush;
        fill ??= Brushes.Black;

        var scaleX = Bounds.Width / 24.0;
        var scaleY = Bounds.Height / 24.0;
        using (context.PushTransform(Matrix.CreateScale(scaleX, scaleY)))
            context.DrawGeometry(fill, null, _geometry);
    }
}
