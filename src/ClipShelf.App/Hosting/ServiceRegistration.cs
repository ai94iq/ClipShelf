using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Features.History;
using ClipShelf.App.Features.Settings;
using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using ClipShelf.Core.Services;
using ClipShelf.Data.Repositories;

namespace ClipShelf.App.Hosting;

public static class ServiceRegistration
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, AppSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton<ISettingsStore, FileSettingsStore>();
        services.AddSingleton<SettingsService>();
        services.AddMemoryCache(o => o.SizeLimit = 2_000);
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

        // Data: factory, initializer and repositories are Singletons.
        services.AddSingleton(new DataOptions(
            AppPaths.Database, AppPaths.Backups, DatabaseKey.LoadOrCreate(AppPaths.DatabaseKey)));
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IClipRepository, ClipRepository>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();

        // Clipboard capture and the tray-driven lifetime.
        services.AddSingleton<ClipCaptureService>();
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<GlobalHotkey>();
        services.AddSingleton<AppLifetime>();
        services.AddSingleton<AppShellService>();
        services.AddSingleton<ClipboardPanelWindow>();
        services.AddTransient<ClipboardPanelViewModel>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<SettingsViewModel>();

        // App services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ICategoryLockService, CategoryLockService>();
        services.AddSingleton<ILockPasswordService, LockPasswordService>();
        services.AddSingleton<IClipExportService>(sp => new ClipExportService(
            sp.GetRequiredService<IClipRepository>(), sp.GetRequiredService<ICategoryLockService>()));
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddSingleton<SessionWatcher>();
        services.AddSingleton<SessionCleanup>();
        services.AddSingleton<IDateFormatter, DateFormatter>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<IUpdateChecker, UpdateChecker>();

        // Shell: Singleton. Pages and their ViewModels: Transient.
        // When adding navigation, register WPF-UI services exactly as the installed WPF-UI 4.x sample does.
        services.AddSingleton<MainWindow>();
        services.AddSingleton<WelcomeWindow>();
        return services;
    }
}
