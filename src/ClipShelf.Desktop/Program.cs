using Avalonia;
using Avalonia.Controls;

namespace ClipShelf.Desktop;

internal static class Program
{
    // Tray-first: closing every window must not end the process, only Exit does.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
