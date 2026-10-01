using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Destructive actions get a red text/icon tint on hover. The Button template drives its hover
// colours through theme brushes, so the brushes are overridden on the button itself.
public static class DestructiveHover
{
    public static void TintOnHover(Button button)
    {
        var critical = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        button.Resources["ButtonForegroundPointerOver"] = critical;
        button.Resources["ButtonForegroundPressed"] = critical;
    }
}
