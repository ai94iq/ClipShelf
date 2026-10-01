using ClipShelf.App.Shell;

namespace ClipShelf.App.Hosting;

public static class ServiceRegistration
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, AppSettings settings)
    {
        services.AddSingleton(settings);
        services.AddMemoryCache(o => o.SizeLimit = 2_000);
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

        // Data: factory, initializer and repositories are Singletons.
        services.AddSingleton(new DataOptions(AppPaths.Database, AppPaths.Backups));
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DatabaseInitializer>();

        // App services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDateFormatter, DateFormatter>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();

        // Shell: Singleton. Pages and their ViewModels: Transient.
        // When adding navigation, register WPF-UI services exactly as the installed WPF-UI 4.x sample does.
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        return services;
    }
}
