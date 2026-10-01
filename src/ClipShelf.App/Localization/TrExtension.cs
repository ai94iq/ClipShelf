using Microsoft.UI.Xaml.Markup;

namespace ClipShelf.App.Localization;

// XAML: xmlns:l="using:ClipShelf.App.Localization", then Text="{l:Tr Key=Common_Save}".
// WinUI markup extensions take named properties only, so "Key=" is required.
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class TrExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    protected override object ProvideValue() => Tr.Get(Key);
}
