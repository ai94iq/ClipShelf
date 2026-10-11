using System.Runtime.Versioning;
using ClipShelf.Desktop.Platform.Linux;
using ClipShelf.Desktop.Views;

namespace ClipShelf.Desktop.Hosting;

// Phase 3 opens with boot: the real libsecret key store plus honest stubs so
// the app starts on Linux and the history window works. Capture, hotkeys,
// paste-back, tray, autostart and session hooks arrive as later tasks; each
// stub names its task.
public static class LinuxServiceRegistration
{
    [SupportedOSPlatform("linux")]
    public static IServiceCollection AddLinuxPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IKeyStore, LibsecretKeyStore>();
        services.AddSingleton<X11ClipboardService>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<X11ClipboardService>());
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<X11ClipboardService>());
        services.AddSingleton<X11HotkeyService>();
        services.AddSingleton<IGlobalHotkey>(sp => sp.GetRequiredService<X11HotkeyService>());
        services.AddSingleton<IPasteBack, LinuxPasteBack>();
        services.AddSingleton<IForegroundApp, X11ForegroundApp>();
        services.AddSingleton<IStartupRegistration, LinuxStartup>();
        services.AddSingleton<ISystemTheme, LinuxSystemTheme>();
        services.AddSingleton<ISessionEnd, LinuxSessionEnd>();
        return services;
    }

    // The windows are portable; the tray and the Windows flyout stay
    // Windows-only until their Linux counterparts land.
    [SupportedOSPlatform("linux")]
    public static IServiceCollection AddLinuxUiServices(this IServiceCollection services)
    {
        services.AddSingleton<ClipboardPanelViewModel>();
        services.AddSingleton<LinuxFlyoutWindow>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<SettingsPage>();
        return services;
    }
}
