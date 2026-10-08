using Avalonia;
using Serilog;

namespace ClipShelf.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();    // the SQLCipher provider must be set up first
        var settings = new FileSettingsStore().Load();
        Culture.Configure(settings);
        AppLogging.Configure();

        var command = StartupCommand.Parse(args);
        var singleInstance = new SingleInstanceService(SingleInstanceService.DefaultName);
        if (!singleInstance.TryClaim())
        {
            if (command is not null)
                SingleInstanceService.TrySendAsync(SingleInstanceService.DefaultName, command)
                    .GetAwaiter().GetResult();
            Log.Information("Second instance blocked");
            Log.CloseAndFlush();
            return;
        }

        App.Settings = settings;
        App.PendingCommand = command;
        App.SingleInstance = singleInstance;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            singleInstance.Dispose();
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
