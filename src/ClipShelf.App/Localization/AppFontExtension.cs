using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Localization;

// XAML: FontFamily="{l:AppFont}" - picks the bundled Arabic font when the UI is right-to-left.
[MarkupExtensionReturnType(ReturnType = typeof(FontFamily))]
public sealed partial class AppFontExtension : MarkupExtension
{
    protected override object ProvideValue() => new FontFamily(AppFonts.Family(Culture.IsRtl));
}
