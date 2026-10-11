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
        services.AddSingleton<LinuxClipboard>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<LinuxClipboard>());
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<LinuxClipboard>());
        services.AddSingleton<LinuxHotkey>();
        services.AddSingleton<IGlobalHotkey>(sp => sp.GetRequiredService<LinuxHotkey>());
        services.AddSingleton<IPasteBack, LinuxPasteBack>();
        services.AddSingleton<IForegroundApp, LinuxForegroundApp>();
        services.AddSingleton<IStartupRegistration, LinuxStartup>();
        services.AddSingleton<ISystemTheme, LinuxSystemTheme>();
        services.AddSingleton<ISessionEnd, LinuxSessionEnd>();
        return services;
    }

    // The windows are portable; only the tray and the flyout stay Windows-only
    // until their Linux counterparts land.
    [SupportedOSPlatform("linux")]
    public static IServiceCollection AddLinuxUiServices(this IServiceCollection services)
    {
        services.AddSingleton<ClipboardPanelViewModel>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<SettingsPage>();
        return services;
    }
}
