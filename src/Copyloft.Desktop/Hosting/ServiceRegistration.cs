using System.Runtime.Versioning;
using Copyloft.Desktop.Features;
using Copyloft.Desktop.Platform;
using Copyloft.Desktop.Platform.Linux;
using Copyloft.Desktop.Platform.MacOS;
using Copyloft.Desktop.Platform.Windows;
using Copyloft.Desktop.Shell;

namespace Copyloft.Desktop.Hosting;

public static class ServiceRegistration
{
    // The OS implementations of the presentation's platform interfaces.
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<IClipboardWriter>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<IClipboardMonitor>(sp => sp.GetRequiredService<ClipboardService>());

        if (OperatingSystem.IsWindows()) return services.AddWindows();
        if (OperatingSystem.IsLinux()) return services.AddLinux();
        if (OperatingSystem.IsMacOS()) return services.AddMacOS();
        throw new PlatformNotSupportedException("Copyloft runs on Windows, Linux and macOS.");
    }

    // X11 (and XWayland); native Wayland sessions get their own services later.
    [SupportedOSPlatform("linux")]
    private static IServiceCollection AddLinux(this IServiceCollection services)
    {
        // Reads the key ClipShelf stored when the Copyloft entry is empty (see LegacyKeyring).
        services.AddSingleton<ISecretKeyring>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<LibSecretKeyring>>();
            return new LegacyKeyring(new LibSecretKeyring(log), new LibSecretKeyring(log, "io.github.ai94iq.ClipShelf"));
        });
        services.AddSingleton<IKeyStore, KeyringKeyStore>();
        services.AddSingleton<X11Connection>();
        services.AddSingleton<IClipboardBackend>(sp =>
        {
            var backend = new AvaloniaClipboardBackend(() => sp.GetRequiredService<FlyoutWindow>().Clipboard, "image/png");
            var ui = sp.GetRequiredService<IUiDispatcher>();
            sp.GetRequiredService<X11Connection>().SelectionChanged += () => ui.Post(backend.NotifyChanged);
            return backend;
        });
        services.AddSingleton<IGlobalHotkey, X11GlobalHotkey>();
        services.AddSingleton<X11Desktop>();
        services.AddSingleton<IPasteBack>(sp => sp.GetRequiredService<X11Desktop>());
        services.AddSingleton<IForegroundApp>(sp => sp.GetRequiredService<X11Desktop>());
        services.AddSingleton<IFlyoutPlatform>(sp => sp.GetRequiredService<X11Desktop>());
        services.AddSingleton<IStartupRegistration>(sp => new AutostartRegistration(
            sp.GetRequiredService<ILogger<AutostartRegistration>>()));
        services.AddSingleton<ISystemTheme, PlatformSystemTheme>();
        services.AddSingleton<ISessionEnd, PosixSessionEnd>();
        return services;
    }

    // Avalonia's native macOS backend plus Carbon, CoreGraphics and the Keychain.
    [SupportedOSPlatform("macos")]
    private static IServiceCollection AddMacOS(this IServiceCollection services)
    {
        // Reads the key ClipShelf stored when the Copyloft entry is empty (see LegacyKeyring).
        services.AddSingleton<ISecretKeyring>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<MacKeychain>>();
            return new LegacyKeyring(new MacKeychain(log), new MacKeychain(log, "ClipShelf"));
        });
        services.AddSingleton<IKeyStore, KeyringKeyStore>();
        services.AddSingleton<PasteboardWatcher>();
        services.AddSingleton<IClipboardBackend>(sp =>
        {
            var backend = new AvaloniaClipboardBackend(() => sp.GetRequiredService<FlyoutWindow>().Clipboard, "public.png");
            sp.GetRequiredService<PasteboardWatcher>().Changed += backend.NotifyChanged;
            return backend;
        });
        services.AddSingleton<IGlobalHotkey, MacGlobalHotkey>();
        services.AddSingleton<MacDesktop>();
        services.AddSingleton<IPasteBack>(sp => sp.GetRequiredService<MacDesktop>());
        services.AddSingleton<IForegroundApp>(sp => sp.GetRequiredService<MacDesktop>());
        services.AddSingleton<IFlyoutPlatform>(sp => sp.GetRequiredService<MacDesktop>());
        services.AddSingleton<IStartupRegistration>(sp => new LaunchAgentRegistration(
            sp.GetRequiredService<ILogger<LaunchAgentRegistration>>()));
        services.AddSingleton<ISystemTheme, PlatformSystemTheme>();
        services.AddSingleton<ISessionEnd, PosixSessionEnd>();
        return services;
    }
    [SupportedOSPlatform("windows")]
    private static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<IKeyStore, DpapiKeyStore>();
        services.AddSingleton<IClipboardBackend, Win32ClipboardBackend>();
        services.AddSingleton<IGlobalHotkey, GlobalHotkey>();
        services.AddSingleton<IPasteBack, KeyboardPaste>();
        services.AddSingleton<IForegroundApp, ForegroundApp>();
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddSingleton<ISystemTheme, SystemThemeService>();
        services.AddSingleton<ISessionEnd, SessionWatcher>();
        services.AddSingleton<IFlyoutPlatform, WindowsFlyoutPlatform>();
        return services;
    }

    // The shell; windows join it in later batches.
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<AppLifetime>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<ClipboardPanelViewModel>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FlyoutWindow>();
        services.AddSingleton<AppShellService>();
        services.AddSingleton<IHotkeyRegistration>(sp => sp.GetRequiredService<AppShellService>());
        return services;
    }
}
