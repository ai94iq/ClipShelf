using System.Globalization;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Services;
using ClipShelf.Core.Models;

namespace ClipShelf.Presentation.Tests;

// The date display follows the current culture's calendar; nothing in app code forces or converts it.
public sealed class DateFormatterTests
{
    [Fact]
    public void Day_dates_follow_the_culture_calendar()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = UmAlQuraArabic();
            var formatter = new DateFormatter();

            var text = formatter.Format(new DateOnly(2026, 1, 2), DatePrecision.Day);

            Assert.DoesNotContain("2026", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Day_dates_stay_gregorian_for_other_cultures()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var formatter = new DateFormatter();

            var text = formatter.Format(new DateOnly(2026, 1, 2), DatePrecision.Day);

            Assert.Contains("2026", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Approximate_years_follow_the_culture_calendar_too()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = UmAlQuraArabic();
            var formatter = new DateFormatter();

            var text = formatter.Format(new DateOnly(2026, 1, 2), DatePrecision.ApproximateYear);

            Assert.DoesNotContain("2026", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // Set explicitly: ar-SA's default calendar differs between Windows (NLS) and Linux (ICU).
    private static CultureInfo UmAlQuraArabic()
    {
        var culture = new CultureInfo("ar-SA");
        culture.DateTimeFormat.Calendar = new UmAlQuraCalendar();
        return culture;
    }
}

// Culture.Configure sets the process-wide default culture, which every thread without its own
// culture reads; run in parallel, other tests (the settings search) would see Arabic strings.
[CollectionDefinition(nameof(ProcessCulture), DisableParallelization = true)]
public sealed class ProcessCulture;

[Collection(nameof(ProcessCulture))]
public sealed class CultureConfigureTests
{
    [Fact]
    public void Configure_leaves_the_cultures_calendar_alone()
    {
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Culture.Configure(new AppSettings { Language = "ar-SA" });

            // The OS data picks the default: Umm al-Qura on Windows (NLS), Gregorian on Linux (ICU).
            Assert.IsType(new CultureInfo("ar-SA").Calendar.GetType(), CultureInfo.CurrentCulture.DateTimeFormat.Calendar);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = defaultUiCulture;
        }
    }
}
