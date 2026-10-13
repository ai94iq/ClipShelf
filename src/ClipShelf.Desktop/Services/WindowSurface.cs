using Avalonia.Controls;
using Avalonia.Media;
using ClipShelf.Core.Theming;

namespace ClipShelf.Desktop.Services;

// Window surfaces and backdrops, as main does them: Light and full black are opaque, Dark and
// System show the chosen backdrop, and the flyout is always acrylic. A window the backdrop does
// not reach (Windows 10, or no compositor) keeps its solid surface instead of turning clear.
internal static class WindowSurface
{
    private static readonly List<(WeakReference<Window> Window, string SurfaceKey, bool Flyout)> Attached = [];
    private static AppSettings? _settings;

    // Windows attach once, in their constructor; they follow every later Apply.
    public static void Attach(Window window, string surfaceKey, bool flyout = false)
    {
        Attached.Add((new WeakReference<Window>(window), surfaceKey, flyout));
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == TopLevel.ActualTransparencyLevelProperty) Paint(window, surfaceKey);
        };
        ApplyTo(window, surfaceKey, flyout);
    }

    // Null (tests, or before startup applied the settings) leaves every window opaque.
    public static void Apply(AppSettings? settings)
    {
        _settings = settings;
        Attached.RemoveAll(entry => !entry.Window.TryGetTarget(out _));
        foreach (var (reference, key, flyout) in Attached)
            if (reference.TryGetTarget(out var window)) ApplyTo(window, key, flyout);
    }

    private static void ApplyTo(Window window, string surfaceKey, bool flyout)
    {
        window.TransparencyLevelHint = Levels(_settings, flyout);
        Paint(window, surfaceKey);
    }

    private static WindowTransparencyLevel[] Levels(AppSettings? settings, bool flyout)
    {
        if (settings is null) return [];
        if (flyout) return [WindowTransparencyLevel.AcrylicBlur];
        if (settings.Theme is AppTheme.Light or AppTheme.White or AppTheme.Black) return [];

        // ponytail: Avalonia has one Mica; Mica Alt shows as Mica until it offers the variant.
        return settings.Backdrop switch
        {
            BackdropKind.Mica or BackdropKind.MicaAlt => [WindowTransparencyLevel.Mica],
            BackdropKind.Acrylic => [WindowTransparencyLevel.AcrylicBlur],
            _ => [],
        };
    }

    private static void Paint(Window window, string surfaceKey)
    {
        if (window.ActualTransparencyLevel == WindowTransparencyLevel.None)
            window.Bind(Window.BackgroundProperty, window.GetResourceObservable(surfaceKey));
        else
            window.Background = Brushes.Transparent;
    }
}
