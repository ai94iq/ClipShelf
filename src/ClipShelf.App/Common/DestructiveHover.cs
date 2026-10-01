using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Destructive actions get a red text/icon tint on hover. The Button template drives its hover
// colours through theme brushes, so the brushes are overridden on the button itself, and the icon
// (which carries its own theme brush) is tinted while the pointer is over the button.
public static class DestructiveHover
{
    public static void TintOnHover(Button button)
    {
        var critical = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        button.Resources["ButtonForegroundPointerOver"] = critical;
        button.Resources["ButtonForegroundPressed"] = critical;

        if (button.Content is not AppIcon icon) return;

        var normal = icon.Foreground;
        button.PointerEntered += (_, _) => icon.Foreground = critical;
        button.PointerExited += (_, _) => icon.Foreground = normal;
    }
}
