using ClipShelf.Desktop.Hosting;
using ClipShelf.Desktop.Platform.Linux;
using ClipShelf.Presentation.Platform;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the Linux platform set: every mapping resolves without
// touching native libraries (libsecret resolves lazily on first key use).
public sealed class LinuxRegistrationTests
{
    [Fact]
    public void LinuxServices_AllResolve()
    {
// Registration maps linux-annotated types; resolving them here never executes
// native code (libsecret resolves lazily on first key use).
#pragma warning disable CA1416
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLinuxPlatformServices();
        services.AddLinuxUiServices();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<LibsecretKeyStore>(provider.GetRequiredService<IKeyStore>());
        Assert.IsType<LinuxClipboard>(provider.GetRequiredService<IClipboardMonitor>());
        Assert.IsType<LinuxClipboard>(provider.GetRequiredService<IClipboardWriter>());
        Assert.IsType<LinuxHotkey>(provider.GetRequiredService<IGlobalHotkey>());
        Assert.IsType<LinuxPasteBack>(provider.GetRequiredService<IPasteBack>());
        Assert.IsType<LinuxForegroundApp>(provider.GetRequiredService<IForegroundApp>());
        Assert.IsType<LinuxSystemTheme>(provider.GetRequiredService<ISystemTheme>());
        Assert.IsType<LinuxSessionEnd>(provider.GetRequiredService<ISessionEnd>());
        Assert.IsType<LinuxStartup>(provider.GetRequiredService<IStartupRegistration>());
#pragma warning restore CA1416
    }
}
