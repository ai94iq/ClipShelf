using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Shell;
using ClipShelf.Desktop.Views;

namespace ClipShelf.Desktop.Hosting;

public static class DesktopServiceRegistration
{
    // The Windows implementations of the presentation's platform interfaces. The Linux and macOS
    // implementations arrive with their phases; everything Windows-only sits behind the guard.
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<AvaloniaThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<AvaloniaThemeService>());

        if (OperatingSystem.IsWindows())
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
            services.AddSingleton<ISessionEnd, SessionWatcher>();
        }

        return services;
    }

    // The windows, pages and the tray.
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();

        if (OperatingSystem.IsWindows())
        {
            services.AddTransient<ClipboardPanelViewModel>();
            services.AddSingleton<FlyoutWindow>();
            services.AddSingleton<HistoryPage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<TrayController>();
            // The lambda defers execution; the analyzer cannot see that this branch is Windows-only.
#pragma warning disable CA1416
            services.AddSingleton<IHotkeyRegistration>(sp => sp.GetRequiredService<TrayController>());
#pragma warning restore CA1416
        }

        return services;
    }
}
