using System.Globalization;
using ClipShelf.App.Hosting;
using ClipShelf.App.Services;
using ClipShelf.Core.Models;

namespace ClipShelf.App.Tests;

// The date display follows the current culture's calendar; nothing in app code forces or converts it.
public sealed class DateFormatterTests
{
    [Fact]
    public void Day_dates_follow_the_culture_calendar()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            var formatter = new DateFormatter();

            var text = formatter.Format(new DateOnly(2026, 1, 2), DatePrecision.Day);

            Assert.DoesNotContain("2026", text);          // ar-SA defaults to Umm al-Qura, not Gregorian
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
    public void Configure_leaves_the_cultures_calendar_alone()
    {
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Culture.Configure(new AppSettings { Language = "ar-SA" });

            Assert.IsType<UmAlQuraCalendar>(CultureInfo.CurrentCulture.DateTimeFormat.Calendar);
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
