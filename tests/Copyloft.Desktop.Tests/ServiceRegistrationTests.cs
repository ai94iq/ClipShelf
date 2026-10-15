using Copyloft.Core.Abstractions;
using Copyloft.Desktop.Hosting;
using Copyloft.Presentation.Platform;
using Copyloft.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Copyloft.Desktop.Tests;

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
        Assert.SkipUnless(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "a supported OS");

        var services = new ServiceCollection().AddPlatformServices();

        Assert.Contains(services, d => d.ServiceType == service);
    }
}
