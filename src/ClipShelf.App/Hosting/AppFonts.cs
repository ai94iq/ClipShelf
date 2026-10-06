namespace ClipShelf.App.Hosting;

// The app font: the system Segoe stack everywhere except an Arabic UI, which uses the bundled
// Noto Sans Arabic. ms-appx resolves the shipped file in unpackaged WinUI apps too.
public static class AppFonts
{
    private const string Segoe = "Segoe UI Variable Text, Segoe UI";
    private const string Noto = "ms-appx:///Assets/Fonts/NotoSansArabic-Regular.ttf#Noto Sans Arabic";

    public static string Family(bool rtl) => rtl ? Noto : Segoe;
}
