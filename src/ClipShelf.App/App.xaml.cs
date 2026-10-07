using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Media;
using Serilog;
using ClipShelf.App.Shell;

namespace ClipShelf.App;

public sealed partial class App : Application
{
    private readonly AppSettings _settings;
    private IHost? _host;
    private Mutex? _singleInstance;
    private bool _ownsMutex;

    public App()
    {
        SQLitePCL.Batteries_V2.Init();            // the SQLCipher provider must be set up first
        _settings = new FileSettingsStore().Load();
        Culture.Configure(_settings);             // before any window or Tr call
        AppLogging.Configure();
        InitializeComponent();

        // The accent is applied in OnLaunched: touching Application.Resources while the app is still
        // being constructed throws E_UNEXPECTED on Windows App SDK 1.8.
        UnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "Unhandled UI exception");
            e.Handled = true;
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _singleInstance = new Mutex(initiallyOwned: true, @"Local\ClipShelf.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            Log.Information("Second instance blocked");
            Exit();
            return;
        }

        try
        {
            ThemeSurfaces.Apply(_settings.Theme);
            ThemeService.ApplyAccentResources(Application.Current.Resources, _settings.Accent);
            // Control text follows the app font too (TextBlocks read the {l:AppFont} extension).
            Application.Current.Resources["ContentControlThemeFontFamily"] =
                new FontFamily(AppFonts.Family(Culture.IsRtl));

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSerilog();        // uses the static Log.Logger
            builder.Services.AddAppServices(_settings);
            _host = builder.Build();
            await _host.StartAsync();
            _host.Services.GetRequiredService<IDataChangeNotifier>();          // must be created on the UI thread

            var database = _host.Services.GetRequiredService<DatabaseInitializer>();
            await Task.Run(database.BackupAndMigrate);

            // SQLCipher derives the key (~0.4 s) on every new physical connection and Dapper warms
            // its mappers on first use; a few pooled connections warmed in the background keep the
            // first copy, flyout open or history open from paying that stall.
            _ = WarmUpAsync(_host.Services);

            // Tray-first: nothing opens at startup. The tray icon and the clipboard watcher keep
            // the app alive in the notification area until the user exits from the tray menu.
            var shell = _host.Services.GetRequiredService<AppShellService>();
            shell.ExitRequested += (_, _) => ExitApplication();
            shell.RestartRequested += (_, _) => RestartApplication();

            // First run: a small window explains the hotkey and the tray, then never again.
            var settingsService = _host.Services.GetRequiredService<SettingsService>();
            if (!settingsService.Current.WelcomeShown)
            {
                _host.Services.GetRequiredService<WelcomeWindow>().Activate();
                settingsService.Update(settingsService.Current with { WelcomeShown = true });
            }

            // Rewrites the startup entry with the current path, so an update that moves the app
            // keeps starting the right executable.
            _host.Services.GetRequiredService<IStartupRegistration>().Apply(_settings.RunAtStartup);

            _host.Services.GetRequiredService<ClipboardService>();
            _host.Services.GetRequiredService<SessionCleanup>();

            // Age-based cleanup runs once per launch.
            if (_settings.RetentionDays > 0)
            {
                var repository = _host.Services.GetRequiredService<IClipRepository>();
                await repository.PruneOlderThanAsync(
                    DateTimeOffset.UtcNow.AddDays(-_settings.RetentionDays), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            Shutdown();
            Exit();
        }
    }

    private void ExitApplication()
    {
        Shutdown();     // disposes the host, which disposes the tray icon and the clipboard watcher
        Exit();
    }

    // Fire-and-forget: the tray appears immediately; the warm-up finishes within a second or two.
    private static async Task WarmUpAsync(IServiceProvider services)
    {
        try
        {
            var clips = services.GetRequiredService<IClipRepository>();
            var categories = services.GetRequiredService<ICategoryRepository>();
            await Task.WhenAll(
                clips.GetRecentAsync(null, 1, CancellationToken.None),
                clips.CountAsync(null, null, null, CancellationToken.None),
                categories.GetCategoriesAsync(CancellationToken.None),
                clips.GetRecentAsync(null, 1, CancellationToken.None));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database warm-up failed");
        }
    }

    // A language change needs a fresh process: release everything (including the single-instance
    // mutex) first, then start a new instance and let this one go.
    private void RestartApplication()
    {
        Shutdown();
        try
        {
            if (Environment.ProcessPath is { } executable) Process.Start(executable);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Restarting the app failed");
        }
        Exit();
    }

    private void Shutdown()
    {
        _host?.Dispose();                         // the app registers no hosted services
        if (_ownsMutex) _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        Log.CloseAndFlush();
    }
}
