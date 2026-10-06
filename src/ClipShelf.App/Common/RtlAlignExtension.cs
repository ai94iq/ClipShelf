using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace ClipShelf.App.Common;

// XAML: HorizontalAlignment="{common:RtlAlign Desired=Left}" - start/end alignment in both flows.
[MarkupExtensionReturnType(ReturnType = typeof(HorizontalAlignment))]
public sealed partial class RtlAlignExtension : MarkupExtension
{
    public HorizontalAlignment Desired { get; set; }

    protected override object ProvideValue() => RtlAlign.For(Desired, Culture.IsRtl);
}
