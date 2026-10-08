using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Views;

namespace ClipShelf.Desktop.Hosting;

public static class DesktopServiceRegistration
{
    // The platform implementations that exist in Task 3. The clipboard, hotkey, paste-back, tray
    // and friends arrive with Task 5; the flyout with Task 6.
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IKeyStore, DpapiKeyStore>();
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<AvaloniaThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<AvaloniaThemeService>());
        return services;
    }

    // The windows and pages. Only the shell exists so far.
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        return services;
    }
}
