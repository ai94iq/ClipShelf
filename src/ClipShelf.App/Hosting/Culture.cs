namespace ClipShelf.App.Hosting;

public static class Culture
{
    public static bool IsRtl { get; private set; }

    // WinUI has no render-time digit substitution, so Arabic-Indic digits are a WPF-only setting.
    public static bool ArabicIndicDigits => false;

    public static void Configure(AppSettings settings)
    {
        var culture = new CultureInfo(settings.Language == "en-US" ? "en-US" : "ar-SA");

        // ar-SA defaults to Umm al-Qura. All formatting uses Gregorian; Hijri is IDateFormatter's job.
        var gregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (gregorian is not null) culture.DateTimeFormat.Calendar = gregorian;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        IsRtl = culture.TextInfo.IsRightToLeft;
    }
}
