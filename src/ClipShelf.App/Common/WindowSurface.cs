using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Window surfaces, shared by the shell, the pages and the flyout.
internal static class WindowSurface
{
    public static void ApplyAcrylic(Window window, Panel root)
    {
        if (DesktopAcrylicController.IsSupported())
        {
            window.SystemBackdrop = new DesktopAcrylicBackdrop();
            root.Background = null;
            return;
        }

        root.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
    }

    public static void ApplyMica(Window window, Panel root)
    {
        if (MicaController.IsSupported())
        {
            window.SystemBackdrop = new MicaBackdrop();
            return;
        }

        root.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
    }
}
