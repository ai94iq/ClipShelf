using System.Runtime.Versioning;
using ClipShelf.Desktop.Features;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Platform.Linux;
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
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<ClipboardService>());

        if (OperatingSystem.IsWindows()) return services.AddWindows();
        if (OperatingSystem.IsLinux()) return services.AddLinux();
        throw new PlatformNotSupportedException("ClipShelf runs on Windows and Linux.");
    }

    // X11 (and XWayland); native Wayland sessions get their own services later.
    [SupportedOSPlatform("linux")]
    private static IServiceCollection AddLinux(this IServiceCollection services)
    {
        services.AddSingleton<ISecretKeyring, LibSecretKeyring>();
        services.AddSingleton<IKeyStore, LinuxKeyStore>();
        services.AddSingleton<X11Connection>();
        services.AddSingleton<IClipboardBackend>(sp => new X11ClipboardBackend(
            sp.GetRequiredService<X11Connection>(),
            sp.GetRequiredService<IUiDispatcher>(),
            () => sp.GetRequiredService<FlyoutWindow>().Clipboard));
        services.AddSingleton<IGlobalHotkey, X11GlobalHotkey>();
        services.AddSingleton<X11Desktop>();
        services.AddSingleton<IPasteBack>(sp => sp.GetRequiredService<X11Desktop>());
        services.AddSingleton<IForegroundApp>(sp => sp.GetRequiredService<X11Desktop>());
        services.AddSingleton<IFlyoutPlatform>(sp => sp.GetRequiredService<X11Desktop>());
        services.AddSingleton<IStartupRegistration>(sp => new AutostartRegistration(
            sp.GetRequiredService<ILogger<AutostartRegistration>>()));
        services.AddSingleton<ISystemTheme, PlatformSystemTheme>();
        services.AddSingleton<ISessionEnd, LinuxSessionEnd>();
        return services;
    }

    [SupportedOSPlatform("windows")]
    private static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<IKeyStore, DpapiKeyStore>();
        services.AddSingleton<IClipboardBackend, Win32ClipboardBackend>();
        services.AddSingleton<IGlobalHotkey, GlobalHotkey>();
        services.AddSingleton<IPasteBack, KeyboardPaste>();
        services.AddSingleton<IForegroundApp, ForegroundApp>();
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddSingleton<ISystemTheme, SystemThemeService>();
        services.AddSingleton<ISessionEnd, SessionWatcher>();
        services.AddSingleton<IFlyoutPlatform, WindowsFlyoutPlatform>();
        return services;
    }

    // The shell; windows join it in later batches.
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<AppLifetime>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<ClipboardPanelViewModel>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FlyoutWindow>();
        services.AddSingleton<AppShellService>();
        services.AddSingleton<IHotkeyRegistration>(sp => sp.GetRequiredService<AppShellService>());
        return services;
    }
}
