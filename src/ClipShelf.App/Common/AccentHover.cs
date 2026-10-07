using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Menu and dropdown options take the soft accent wash on hover. The keys are written onto the root
// resources at startup: the same keys declared in App.xaml lose to the framework's theme
// dictionaries, while a runtime entry on the root dictionary wins (the app font relies on this too).
internal static class AccentHover
{
    private static readonly (string Key, double Opacity)[] Entries =
    [
        ("ButtonBackgroundPointerOver", 0.25),
        ("ButtonBackgroundPressed", 0.35),
        ("MenuFlyoutItemBackgroundPointerOver", 0.25),
        ("MenuFlyoutItemBackgroundPressed", 0.35),
        ("ToggleMenuFlyoutItemBackgroundPointerOver", 0.25),
        ("ToggleMenuFlyoutItemBackgroundPressed", 0.35),
        ("ComboBoxItemBackgroundPointerOver", 0.25),
        ("ComboBoxItemBackgroundPressed", 0.35),
    ];

    public static void Apply(ResourceDictionary resources)
    {
        if (resources["SystemAccentColor"] is not Windows.UI.Color accent) return;

        foreach (var (key, opacity) in Entries)
            resources[key] = new SolidColorBrush(accent) { Opacity = opacity };
    }
}
