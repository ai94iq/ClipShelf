using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Features.History;
using ClipShelf.App.Features.Settings;
using ClipShelf.App.Platform;
using ClipShelf.App.Shell;

namespace ClipShelf.App.Hosting;

public static class ServiceRegistration
{
    // The Windows implementations of the presentation's platform interfaces.
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IKeyStore, DpapiKeyStore>();
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<GlobalHotkey>();
        services.AddSingleton<IGlobalHotkey>(sp => sp.GetRequiredService<GlobalHotkey>());
        services.AddSingleton<IPasteBack, KeyboardPaste>();
        services.AddSingleton<IForegroundApp, ForegroundApp>();
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddSingleton<ISystemTheme, SystemThemeService>();
        services.AddSingleton<IUiDispatcher, DispatcherQueueUiDispatcher>();
        services.AddSingleton<ISessionEnd, SessionWatcher>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
        return services;
    }

    // The windows, pages and the shell.
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<AppLifetime>();
        services.AddSingleton<AppShellService>();
        services.AddSingleton<IHotkeyRegistration>(sp => sp.GetRequiredService<AppShellService>());
        services.AddSingleton<ClipboardPanelWindow>();
        services.AddTransient<ClipboardPanelViewModel>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<WelcomeWindow>();
        return services;
    }
}
