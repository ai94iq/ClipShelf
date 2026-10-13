using ClipShelf.Core.Abstractions;
using ClipShelf.Desktop.Hosting;
using ClipShelf.Presentation.Platform;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClipShelf.Desktop.Tests;

public sealed class ServiceRegistrationTests
{
    // Everything Presentation's AddPresentationServices leaves to the head (see its comment).
    [Theory]
    [InlineData(typeof(IKeyStore))]
    [InlineData(typeof(IClipboardMonitor))]
    [InlineData(typeof(IClipboardWriter))]
    [InlineData(typeof(IGlobalHotkey))]
    [InlineData(typeof(IPasteBack))]
    [InlineData(typeof(IForegroundApp))]
    [InlineData(typeof(IStartupRegistration))]
    [InlineData(typeof(ISystemTheme))]
    [InlineData(typeof(IUiDispatcher))]
    [InlineData(typeof(ISessionEnd))]
    [InlineData(typeof(IDataChangeNotifier))]
    [InlineData(typeof(IThemeService))]
    public void The_head_registers_every_platform_service(Type service)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows implementations");

        var services = new ServiceCollection().AddPlatformServices();

        Assert.Contains(services, d => d.ServiceType == service);
    }
}
