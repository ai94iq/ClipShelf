using Avalonia;
using Avalonia.Media;

namespace Copyloft.Desktop.Services;

// The app font, as main: the Segoe UI Variable stack everywhere except an Arabic UI, which uses
// the bundled Noto Sans Arabic (OFL, Assets/Fonts). Controls read Fluent's
// ContentControlThemeFontFamily key; $Default falls back to the platform font off Windows.
internal static class AppFonts
{
    private static readonly FontFamily Segoe = new("Segoe UI Variable Text, Segoe UI, $Default");
    private static readonly FontFamily Noto = new("avares://Copyloft/Assets/Fonts#Noto Sans Arabic, $Default");

    public static FontFamily Family(bool rtl) => rtl ? Noto : Segoe;

    public static void Apply(Application app, bool rtl) => app.Resources["ContentControlThemeFontFamily"] = Family(rtl);
}
