using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using ClipShelf.App.Resources;

namespace ClipShelf.App.Common;

// XAML: Data="{common:IconData Kind=Document}" - the same Fluent geometry the AppIcon control
// draws, for slots that need an IconElement (for example NavigationViewItem.Icon).
[MarkupExtensionReturnType(ReturnType = typeof(Geometry))]
public sealed partial class IconDataExtension : MarkupExtension
{
    public IconKind Kind { get; set; }

    protected override object ProvideValue() =>
        (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), IconData.Get(Kind));
}
