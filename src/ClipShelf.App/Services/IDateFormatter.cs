namespace ClipShelf.App.Services;

public interface IDateFormatter
{
    string Format(DateOnly date, DatePrecision precision);

    // Compact "time ago" text for lists: now, 5m, 2h, 3d, then the date.
    string FormatSince(DateTimeOffset valueUtc);
}
