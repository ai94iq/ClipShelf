using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Desktop.Common;

// XAML: HorizontalAlignment="{common:RtlAlign Desired=Left}" - start/end alignment in both flows.
public sealed class RtlAlignExtension : MarkupExtension
{
    public HorizontalAlignment Desired { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        RtlAlign.For(Desired, Culture.IsRtl);
}
