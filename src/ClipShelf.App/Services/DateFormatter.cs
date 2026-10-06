namespace ClipShelf.App.Services;

// The only place user-facing dates are formatted. Never use StringFormat for dates in XAML.
// The culture's calendar decides the display: ar-SA shows Umm al-Qura dates, other cultures Gregorian.
public sealed class DateFormatter : IDateFormatter
{
    public string Format(DateOnly date, DatePrecision precision)
    {
        if (precision == DatePrecision.Unknown) return string.Empty;
        var value = date.ToDateTime(TimeOnly.MinValue);
        return precision switch
        {
            DatePrecision.Day => value.ToString("d MMMM yyyy", CultureInfo.CurrentCulture),
            DatePrecision.Month => value.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            DatePrecision.ApproximateYear => Tr.Format("Date_Approx", Year(value)),
            _ => Year(value).ToString(CultureInfo.InvariantCulture),
        };
    }

    private static int Year(DateTime value) => CultureInfo.CurrentCulture.Calendar.GetYear(value);

    public string FormatSince(DateTimeOffset valueUtc)
    {
        var elapsed = DateTimeOffset.UtcNow - valueUtc;
        if (elapsed < TimeSpan.FromMinutes(1)) return Tr.Get("Clip_Time_Now");
        if (elapsed < TimeSpan.FromHours(1)) return Tr.Format("Clip_Time_Minutes", (int)elapsed.TotalMinutes);
        if (elapsed < TimeSpan.FromDays(1)) return Tr.Format("Clip_Time_Hours", (int)elapsed.TotalHours);
        if (elapsed < TimeSpan.FromDays(7)) return Tr.Format("Clip_Time_Days", (int)elapsed.TotalDays);
        return Format(DateOnly.FromDateTime(valueUtc.ToLocalTime().DateTime), DatePrecision.Day);
    }
}
