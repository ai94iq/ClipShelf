using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ClipShelf.Desktop.Resources;

namespace ClipShelf.Desktop.Controls;

// A Fluent outline icon by name: <controls:AppIcon Kind="Settings" />.
public sealed class AppIcon : PathIcon
{
    public static readonly StyledProperty<IconKind> KindProperty =
        AvaloniaProperty.Register<AppIcon, IconKind>(nameof(Kind));

    static AppIcon() =>
        KindProperty.Changed.AddClassHandler<AppIcon>((icon, _) => icon.Data = StreamGeometry.Parse(IconData.Get(icon.Kind)));

    public IconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(PathIcon);
}
