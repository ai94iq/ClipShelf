using Serilog;

namespace ClipShelf.App.Hosting;

public static class AppLogging
{
    // Daily rolling files in %LOCALAPPDATA%\ClipShelf\logs, written off the UI thread.
    public static void Configure()
    {
        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.File(
                Path.Combine(AppPaths.Logs, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatProvider: CultureInfo.InvariantCulture));   // never Hijri or Arabic digits in logs
#if DEBUG
        config.MinimumLevel.Debug();
#else
        config.MinimumLevel.Information();
#endif
        Log.Logger = config.CreateLogger();
    }
}
