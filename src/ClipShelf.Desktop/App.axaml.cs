using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClipShelf.Desktop.Hosting;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Shell;
using ClipShelf.Desktop.Views;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ClipShelf.Desktop;

public partial class App : Application
{
    // Set by Program.Main before the lifetime starts: the loaded settings, the launch command and
    // the claimed single-instance pipe.
    public static AppSettings? Settings { get; set; }

    public static string? PendingCommand { get; set; }

    public static SingleInstanceService? SingleInstance { get; set; }

    private IHost? _host;
    private string? _command;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _command = PendingCommand;
        if (SingleInstance is { } instance)
            instance.CommandReceived += OnCommandReceived;

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        var settings = Settings!;
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSerilog();
        builder.Services.AddPresentationServices(settings);
        builder.Services.AddPlatformServices();
        builder.Services.AddUiServices();
        _host = builder.Build();
        _host.Start();

        _host.Services.GetRequiredService<IThemeService>().Apply(settings);

        var database = _host.Services.GetRequiredService<DatabaseInitializer>();
        Task.Run(database.BackupAndMigrate).GetAwaiter().GetResult();

        _ = WarmUpAsync(_host.Services);

        // Tray-first: nothing opens at startup. The tray icon, the hotkey and the clipboard
        // watcher keep the app alive; the window opens from the tray, the hotkey or a launch
        // command, and exiting goes through the tray menu.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        desktop.ShutdownRequested += (_, _) =>
        {
            _host?.Dispose();
            Log.CloseAndFlush();
        };
        if (OperatingSystem.IsWindows())
        {
            _host.Services.GetRequiredService<TrayController>();
            _host.Services.GetRequiredService<FlyoutWindow>();   // created now so the first open is instant
            _host.Services.GetRequiredService<ClipboardService>();
            desktop.Startup += (_, _) =>
            {
                if (OperatingSystem.IsWindows())
                    _host.Services.GetRequiredService<TrayController>().Attach();
            };
        }

        _host.Services.GetRequiredService<SessionCleanup>();
        _host.Services.GetRequiredService<IStartupRegistration>().Apply(settings.RunAtStartup);

        if (settings.RetentionDays > 0)
            _ = PruneAsync(_host.Services, settings.RetentionDays);

        ExecuteCommand(_command);
        _command = null;

        ShowWelcomeIfFirstRun();

        base.OnFrameworkInitializationCompleted();
    }

    // First run: a small window explains the hotkey and the tray, then never again.
    private void ShowWelcomeIfFirstRun()
    {
        if (_host is null) return;
        var settings = _host.Services.GetRequiredService<SettingsService>();
        if (settings.Current.WelcomeShown) return;
        _host.Services.GetRequiredService<WelcomeWindow>().Show();
        settings.Update(settings.Current with { WelcomeShown = true });
    }

    private void OnCommandReceived(object? sender, string received)
    {
        var dispatcher = _host?.Services.GetService<IUiDispatcher>();
        if (dispatcher is null)
        {
            _command = received;
            return;
        }

        dispatcher.Post(() => ExecuteCommand(received));
    }

    private void ExecuteCommand(string? command)
    {
        if (command is null || _host is null || !OperatingSystem.IsWindows()) return;

        var tray = _host.Services.GetRequiredService<TrayController>();
        if (command == StartupCommand.History) tray.ShowHistory();
        else if (command == StartupCommand.Toggle) tray.TogglePanel();
        else if (command == StartupCommand.Quit) tray.Exit();
    }

    // Fire-and-forget: the tray appears immediately; the warm-up finishes within a second.
    private static async Task WarmUpAsync(IServiceProvider services)
    {
        try
        {
            var clips = services.GetRequiredService<IClipRepository>();
            var categories = services.GetRequiredService<ICategoryRepository>();
            await Task.WhenAll(
                clips.GetRecentAsync(null, 1, CancellationToken.None),
                clips.CountAsync(null, null, null, CancellationToken.None),
                categories.GetCategoriesAsync(CancellationToken.None));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database warm-up failed");
        }
    }

    // Age-based cleanup runs once per launch.
    private static async Task PruneAsync(IServiceProvider services, int days)
    {
        try
        {
            var repository = services.GetRequiredService<IClipRepository>();
            await repository.PruneOlderThanAsync(
                DateTimeOffset.UtcNow.AddDays(-days), CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Age-based cleanup failed");
        }
    }
}
