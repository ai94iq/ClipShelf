using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Common;

// Window surfaces and backdrops, shared by the shell, the pages and the flyout.
internal static class WindowSurface
{
    // Applies the chosen backdrop; unsupported backdrops (Windows 10) fall back to the solid
    // theme background so the window is never transparent.
    public static void ApplyBackdrop(Window window, Panel root, BackdropKind kind)
    {
        var backdrop = CreateBackdrop(kind);
        window.SystemBackdrop = backdrop;
        root.Background = backdrop is null
            ? (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]
            : null;
    }

    public static void ApplyAcrylic(Window window, Panel root)
    {
        var backdrop = CreateBackdrop(BackdropKind.Acrylic);
        window.SystemBackdrop = backdrop;
        root.Background = backdrop is null
            ? (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
            : null;
    }

    private static SystemBackdrop? CreateBackdrop(BackdropKind kind) => kind switch
    {
        BackdropKind.Mica when MicaController.IsSupported() => new MicaBackdrop(),
        BackdropKind.MicaAlt when MicaController.IsSupported() => new MicaBackdrop { Kind = MicaKind.BaseAlt },
        BackdropKind.Acrylic when DesktopAcrylicController.IsSupported() => new DesktopAcrylicBackdrop(),
        _ => null,
    };
}
