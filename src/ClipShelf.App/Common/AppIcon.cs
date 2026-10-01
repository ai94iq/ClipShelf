using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using ClipShelf.App.Resources;

namespace ClipShelf.App.Common;

// Outline icon drawn from Resources/Icons.g.cs. Usage: <common:AppIcon Kind="Search" />.
// Takes the surrounding text color. Not mirrored in RTL unless IsDirectional="True"
// (use that for arrows, chevrons, back/forward, send). Default size 20.
public sealed partial class AppIcon : UserControl
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(IconKind), typeof(AppIcon), new PropertyMetadata(default(IconKind), OnKindChanged));

    public static readonly DependencyProperty IsDirectionalProperty = DependencyProperty.Register(
        nameof(IsDirectional), typeof(bool), typeof(AppIcon), new PropertyMetadata(false, OnDirectionalChanged));

    private readonly Microsoft.UI.Xaml.Shapes.Path _path = new() { Width = 24, Height = 24 };

    public AppIcon()
    {
        Width = 20;
        Height = 20;
        IsTabStop = false;
        FlowDirection = FlowDirection.LeftToRight;
        _path.SetBinding(Shape.FillProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
        Content = new Viewbox { Child = _path };
        UpdateGeometry();
    }

    public IconKind Kind
    {
        get => (IconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public bool IsDirectional
    {
        get => (bool)GetValue(IsDirectionalProperty);
        set => SetValue(IsDirectionalProperty, value);
    }

    // WinUI geometries can't be shared between elements, so each icon parses its own.
    private void UpdateGeometry() =>
        _path.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), IconData.Get(Kind));

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((AppIcon)d).UpdateGeometry();

    private static void OnDirectionalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var icon = (AppIcon)d;
        if ((bool)e.NewValue) icon.ClearValue(FlowDirectionProperty);   // inherit, so it mirrors in RTL
        else icon.FlowDirection = FlowDirection.LeftToRight;
    }
}
