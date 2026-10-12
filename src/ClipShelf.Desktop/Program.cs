using Avalonia;
using Avalonia.Controls;
using Serilog;

namespace ClipShelf.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();            // the SQLCipher provider must be set up first
        var settings = new FileSettingsStore().Load();
        Culture.Configure(settings);             // before any window or Tr call
        AppLogging.Configure();

        var command = StartupCommand.Parse(Environment.GetCommandLineArgs());
        using var singleInstance = new SingleInstanceService(SingleInstanceService.DefaultName);
        if (!singleInstance.TryClaim())
        {
            // A second launch hands its command (--toggle, --history) to the running app and leaves.
            if (command is not null)
                SingleInstanceService.TrySendAsync(SingleInstanceService.DefaultName, command)
                    .GetAwaiter().GetResult();
            Log.Information("Second instance blocked");
            Log.CloseAndFlush();
            return;
        }

        App.Launch = new LaunchContext(settings, singleInstance, command);
        try
        {
            // Tray-first: closing every window must not end the process, only Exit does.
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // WinUI composition renders at the fastest panel's rate (measured 2026-10-12: about 139 fps
    // with the window on a 75 Hz or a 144 Hz screen; every mode measured the same). Pinned so a
    // changed Avalonia default cannot quietly lower it; the fallbacks keep older GPUs working.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                CompositionMode =
                [
                    Win32CompositionMode.WinUIComposition,
                    Win32CompositionMode.DirectComposition,
                    Win32CompositionMode.RedirectionSurface,
                ],
                RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Software],
            })
            .LogToTrace();
}

// What Main resolved before Avalonia started; the headless tests run without one.
internal sealed record LaunchContext(AppSettings Settings, SingleInstanceService SingleInstance, string? Command);
