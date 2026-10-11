using ClipShelf.Desktop.Hosting;
using ClipShelf.Desktop.Platform.Linux;
using ClipShelf.Presentation.Platform;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the Linux platform set: every interface maps to its
// Linux implementation. Mapping only, never instantiated: resolving the graphs
// would touch the keyring and the database.
public sealed class LinuxRegistrationTests
{
    [Fact]
    public void LinuxServices_AllMapped()
    {
// Registration maps linux-annotated types; nothing here executes native code.
#pragma warning disable CA1416
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLinuxPlatformServices();
        services.AddLinuxUiServices();

        Maps<IKeyStore, LibsecretKeyStore>(services);
        Maps<IClipboardMonitor, X11ClipboardService>(services);
        Maps<IClipboardWriter, X11ClipboardService>(services);
        Maps<IGlobalHotkey, X11HotkeyService>(services);
        Maps<IPasteBack, LinuxPasteBack>(services);
        Maps<IForegroundApp, X11ForegroundApp>(services);
        Maps<ISystemTheme, LinuxSystemTheme>(services);
        Maps<ISessionEnd, LinuxSessionEnd>(services);
        Maps<IStartupRegistration, LinuxStartup>(services);
#pragma warning restore CA1416
    }

    private static void Maps<TService, TImpl>(IServiceCollection services) =>
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            (descriptor.ImplementationType == typeof(TImpl) ||
                descriptor.ImplementationFactory is not null));
}
