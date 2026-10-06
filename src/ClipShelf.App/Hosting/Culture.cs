namespace ClipShelf.App.Hosting;

public static class Culture
{
    public static bool IsRtl { get; private set; }

    public static void Configure(AppSettings settings)
    {
        var culture = new CultureInfo(settings.Language == "en-US" ? "en-US" : "ar-SA");

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        IsRtl = culture.TextInfo.IsRightToLeft;
    }
}
