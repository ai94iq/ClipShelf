using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Copyloft.Core.Services;
using Copyloft.Desktop.Hosting;
using Copyloft.Desktop.Platform;
using Copyloft.Desktop.Shell;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Copyloft.Desktop;

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

        // The app menu macOS shows next to the Apple menu while the main window is open
        // (MacAppPresence); set before startup, or Avalonia keeps its default "About Avalonia".
        if (OperatingSystem.IsMacOS())
        {
            NativeMenu.SetMenu(this,
            [
                MenuItem("Tray_OpenHistory", shell => shell.ShowHistory()),
                MenuItem("Tray_Settings", shell => shell.ShowSettings(), "Meta+OemComma"),
            ]);
        }
    }

    private NativeMenuItem MenuItem(string textKey, Action<AppShellService> action, string? gesture = null)
    {
        var item = new NativeMenuItem(Tr.Get(textKey));
        if (gesture is not null) item.Gesture = Avalonia.Input.KeyGesture.Parse(gesture);
        item.Click += (_, _) =>
        {
            if (_host is not null) action(_host.Services.GetRequiredService<AppShellService>());
        };
        return item;
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
            if (launch.ClipShelfRunning) throw new LegacyInstanceRunningException();

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
            desktop.ShutdownRequested += (_, _) => shell.PrepareExit();
            shell.RestartRequested += (_, _) =>
            {
                RestartOnExit = true;
                shell.Exit();
            };
            shell.ShowTray();
            // Without LSUIElement, ShowInDock off did not hold: reopened at login, the app sat in
            // the Dock with no window. The main window switches it to regular while open.
            if (OperatingSystem.IsMacOS()) Platform.MacOS.MacAppPresence.Set(regular: false);
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
                    host.Services.GetRequiredService<SettingsService>(), hasTray, globalHotkey: !DesktopSession.IsWayland,
                    explainAccessibility: OperatingSystem.IsMacOS()));

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
            try
            {
                var error = new StartupErrorWindow(ex);
                error.Closed += (_, _) => Exit(desktop);
                error.Show();
            }
            catch (Exception shown)
            {
                Log.Error(shown, "Showing the startup error failed");
                Exit(desktop);
            }
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
