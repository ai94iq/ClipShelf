using Avalonia.Markup.Xaml;

namespace ClipShelf.Desktop.Common;

// {l:Tr Key=...} for XAML copy, the same shape the WinUI build used.
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => Tr.Get(Key);
}
