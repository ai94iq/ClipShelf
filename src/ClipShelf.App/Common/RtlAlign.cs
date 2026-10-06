using Microsoft.UI.Xaml;

namespace ClipShelf.App.Common;

// Start/end alignment for RTL: HorizontalAlignment stays physical in the XAML layout, so the
// spots that mean "start" or "end" flip explicitly when the UI is right-to-left.
public static class RtlAlign
{
    public static HorizontalAlignment For(HorizontalAlignment desired, bool rtl) =>
        rtl
            ? desired switch
            {
                HorizontalAlignment.Left => HorizontalAlignment.Right,
                HorizontalAlignment.Right => HorizontalAlignment.Left,
                _ => desired,
            }
            : desired;
}
