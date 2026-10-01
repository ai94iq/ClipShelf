namespace ClipShelf.App.Services;

// The only place user-facing dates are formatted. Never use StringFormat for dates in XAML.
public sealed class DateFormatter(AppSettings settings) : IDateFormatter
{
    private static readonly UmAlQuraCalendar UmAlQura = new();
    private static readonly HijriCalendar Tabular = new();

    public string Format(DateOnly date, DatePrecision precision)
    {
        if (precision == DatePrecision.Unknown) return string.Empty;
        var value = date.ToDateTime(TimeOnly.MinValue);
        return settings.UseHijri ? FormatHijri(value, precision) : FormatGregorian(value, precision);
    }

    public string FormatSince(DateTimeOffset valueUtc)
    {
        var elapsed = DateTimeOffset.UtcNow - valueUtc;
        if (elapsed < TimeSpan.FromMinutes(1)) return Tr.Get("Clip_Time_Now");
        if (elapsed < TimeSpan.FromHours(1)) return Tr.Format("Clip_Time_Minutes", (int)elapsed.TotalMinutes);
        if (elapsed < TimeSpan.FromDays(1)) return Tr.Format("Clip_Time_Hours", (int)elapsed.TotalHours);
        if (elapsed < TimeSpan.FromDays(7)) return Tr.Format("Clip_Time_Days", (int)elapsed.TotalDays);
        return Format(DateOnly.FromDateTime(valueUtc.ToLocalTime().DateTime), DatePrecision.Day);
    }

    private static string FormatGregorian(DateTime value, DatePrecision precision) => precision switch
    {
        DatePrecision.Day => value.ToString("d MMMM yyyy", CultureInfo.CurrentCulture),
        DatePrecision.Month => value.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
        DatePrecision.ApproximateYear => Tr.Format("Date_Approx", value.Year),
        _ => value.Year.ToString(CultureInfo.InvariantCulture),
    };

    private static string FormatHijri(DateTime value, DatePrecision precision)
    {
        // Umm al-Qura covers ~1900–2077 CE only; older ancestors use the tabular Hijri calendar.
        Calendar calendar = value >= UmAlQura.MinSupportedDateTime && value <= UmAlQura.MaxSupportedDateTime
            ? UmAlQura
            : Tabular;
        int year = calendar.GetYear(value), month = calendar.GetMonth(value), day = calendar.GetDayOfMonth(value);
        var monthName = Tr.Get(string.Create(CultureInfo.InvariantCulture, $"HijriMonth_{month}"));
        return precision switch
        {
            DatePrecision.Day => Tr.Format("Date_HijriDay", day, monthName, year),
            DatePrecision.Month => Tr.Format("Date_HijriMonth", monthName, year),
            DatePrecision.ApproximateYear => Tr.Format("Date_HijriApprox", year),
            _ => Tr.Format("Date_HijriYear", year),
        };
    }
}
