using ClipShelf.Desktop.Hosting;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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

        var shell = _host.Services.GetRequiredService<MainWindow>();
        desktop.MainWindow = shell;

        ExecuteCommand(_command);
        _command = null;

        base.OnFrameworkInitializationCompleted();
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
        if (command is null || _host is null) return;

        var shell = _host.Services.GetRequiredService<MainWindow>();
        if (command == StartupCommand.History) shell.ShowHistory();
        // --toggle lands with the flyout in Task 6; the command is accepted and stashed until then.
    }

    // Fire-and-forget: the window appears immediately; the warm-up finishes within a second.
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
}
