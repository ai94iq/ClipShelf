using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClipShelf.Core.Services;
using ClipShelf.Desktop.Hosting;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Shell;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ClipShelf.Desktop;

public sealed partial class App : Application
{
    private IHost? _host;
    private string? _pendingCommand;

    internal static LaunchContext? Launch { get; set; }

    // Set when the user asked for a restart; Program starts the new process once this one has
    // let go of the single-instance pipe.
    internal static bool RestartOnExit { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppFonts.Apply(this, Culture.IsRtl);     // Culture is set in Program, before Avalonia starts
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && Launch is { } launch)
            _ = StartAsync(desktop, launch);
    }

    private async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop, LaunchContext launch)
    {
        // Commands can arrive while startup is still running; they wait until the shell exists.
        _pendingCommand = launch.Command;
        launch.SingleInstance.CommandReceived += (_, received) => Dispatcher.UIThread.Post(() =>
        {
            if (_host is null) _pendingCommand = received;
            else ExecuteCommand(received);
        });

        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSerilog();        // uses the static Log.Logger
            builder.Services.AddPresentationServices(launch.Settings);
            builder.Services.AddPlatformServices();
            builder.Services.AddUiServices();
            var host = builder.Build();
            await host.StartAsync();

            host.Services.GetRequiredService<IThemeService>().Apply(launch.Settings);
            var database = host.Services.GetRequiredService<DatabaseInitializer>();
            await Task.Run(database.BackupAndMigrate);

            // SQLCipher derives the key on every new physical connection; warming a few pooled
            // connections keeps the first copy or flyout open from paying that stall.
            _ = WarmUpAsync(host.Services);

            // Tray-first: nothing opens at startup.
            var shell = host.Services.GetRequiredService<AppShellService>();
            shell.ExitRequested += (_, _) => Exit(desktop);
            shell.RestartRequested += (_, _) =>
            {
                RestartOnExit = true;
                shell.Exit();
            };
            shell.ShowTray();
            _host = host;

            // Without a tray (GNOME without the AppIndicator extension) the app would start
            // invisible; History opens instead, and the hotkey or --toggle reach the flyout.
            var hasTray = TrayHost.IsAvailable();
            if (_pendingCommand is null && !hasTray)
            {
                Log.Warning("No tray host on this desktop; opening History at launch");
                _pendingCommand = StartupCommand.History;
            }

            ExecuteCommand(_pendingCommand);
            _pendingCommand = null;

            // First run: a small window explains the hotkey and the tray, then never again.
            WelcomeWindow.ShowOnFirstRun(
                host.Services.GetRequiredService<SettingsService>(),
                () => new WelcomeWindow(
                    host.Services.GetRequiredService<SettingsService>(), hasTray, globalHotkey: !DesktopSession.IsWayland));

            // Rewrites the startup entry with the current path, so a moved install keeps working.
            host.Services.GetRequiredService<IStartupRegistration>().Apply(launch.Settings.RunAtStartup);

            host.Services.GetRequiredService<IClipboardMonitor>();
            host.Services.GetRequiredService<SessionCleanup>();

            // Age-based cleanup runs once per launch.
            if (launch.Settings.RetentionDays > 0)
            {
                await host.Services.GetRequiredService<IClipRepository>().PruneOlderThanAsync(
                    DateTimeOffset.UtcNow.AddDays(-launch.Settings.RetentionDays), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            Exit(desktop);
        }
    }

    private void ExecuteCommand(string? command)
    {
        if (command is null || _host is null) return;

        var shell = _host.Services.GetRequiredService<AppShellService>();
        if (command == StartupCommand.History) shell.ShowHistory();
        else if (command == StartupCommand.Toggle) shell.TogglePanel();
    }

    // Disposing the host disposes the tray icon, the hotkey and the clipboard watcher.
    private void Exit(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _host?.Dispose();
        _host = null;
        desktop.Shutdown();
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
}
