using Microsoft.Extensions.Hosting;
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
            if (_settings.Accent is not null) ThemeService.ApplyAccentResources(Resources, _settings.Accent);

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSerilog();        // uses the static Log.Logger
            builder.Services.AddAppServices(_settings);
            _host = builder.Build();
            await _host.StartAsync();
            _host.Services.GetRequiredService<IDataChangeNotifier>();          // must be created on the UI thread

            var database = _host.Services.GetRequiredService<DatabaseInitializer>();
            await Task.Run(database.BackupAndMigrate);

            // Tray-first: nothing opens at startup. The tray icon and the clipboard watcher keep
            // the app alive in the notification area until the user exits from the tray menu.
            var shell = _host.Services.GetRequiredService<AppShellService>();
            shell.ExitRequested += (_, _) => ExitApplication();
            _host.Services.GetRequiredService<ClipboardService>();
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

    private void Shutdown()
    {
        _host?.Dispose();                         // the app registers no hosted services
        if (_ownsMutex) _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        Log.CloseAndFlush();
    }
}
