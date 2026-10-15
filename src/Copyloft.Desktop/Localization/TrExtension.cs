using Avalonia.Markup.Xaml;

namespace Copyloft.Desktop.Localization;

// XAML text from the shared resources: Text="{l:Tr Key=App_Name}". Resolved once; the language
// changes with a restart, as in the WinUI app.
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => Tr.Get(Key);
}
