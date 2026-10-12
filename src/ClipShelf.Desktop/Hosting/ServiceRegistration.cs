using System.Runtime.Versioning;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Hosting;

public static class ServiceRegistration
{
    // The OS implementations of the presentation's platform interfaces. Windows only for now;
    // Linux and macOS register theirs here after the swap.
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("ClipShelf runs on Windows until the Linux phase.");
        return services.AddWindows();
    }

    [SupportedOSPlatform("windows")]
    private static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<IKeyStore, DpapiKeyStore>();
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<IGlobalHotkey, GlobalHotkey>();
        services.AddSingleton<IPasteBack, KeyboardPaste>();
        services.AddSingleton<IForegroundApp, ForegroundApp>();
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddSingleton<ISystemTheme, SystemThemeService>();
        services.AddSingleton<ISessionEnd, SessionWatcher>();
        return services;
    }

    // The shell; windows join it in later batches.
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<AppLifetime>();
        services.AddSingleton<AppShellService>();
        services.AddSingleton<IHotkeyRegistration>(sp => sp.GetRequiredService<AppShellService>());
        return services;
    }
}
