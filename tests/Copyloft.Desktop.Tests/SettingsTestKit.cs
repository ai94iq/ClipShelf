using Copyloft.Core.Abstractions;
using Copyloft.Core.Models;
using Copyloft.Desktop.Features;
using Copyloft.Desktop.Platform;
using Copyloft.Desktop.Shell;
using Copyloft.Presentation.Features.Settings;
using Copyloft.Presentation.Hosting;
using Copyloft.Presentation.Platform;
using Copyloft.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Copyloft.Desktop.Tests;

// A real SettingsViewModel and page over faked services; the shell is real but has no windows.
internal sealed class SettingsKit
{
    public SettingsKit(AppSettings? settings = null, bool hotkeyRegistered = true, bool passwordSet = false)
    {
        Settings = new SettingsService(Substitute.For<ISettingsStore>(), settings ?? new AppSettings());
        Hotkeys.IsCurrentHotkeyRegistered.Returns(hotkeyRegistered);
        Passwords.IsSet.Returns(passwordSet);
        Categories.GetCategoriesAsync(default).ReturnsForAnyArgs(_ => Task.FromResult<IReadOnlyList<Category>>(StoredCategories));
        ViewModel = new SettingsViewModel(
            Settings, Theme, Repository, Startup, Export, Categories, Locks, Passwords, Updates,
            new Lazy<IHotkeyRegistration>(() => Hotkeys), NullLogger<SettingsViewModel>.Instance);
        Shell = new AppShellService(
            Substitute.For<IGlobalHotkey>(), Settings, Substitute.For<ISystemTheme>(), Locks, new AppLifetime(),
            new Lazy<MainWindow>(() => throw new InvalidOperationException("no main window in this test")),
            new Lazy<FlyoutWindow>(() => throw new InvalidOperationException("no flyout in this test")),
            NullLogger<AppShellService>.Instance);
        Page = new SettingsPage(ViewModel, new Lazy<AppShellService>(() => Shell));
    }

    public SettingsService Settings { get; }
    public IThemeService Theme { get; } = Substitute.For<IThemeService>();
    public IClipRepository Repository { get; } = Substitute.For<IClipRepository>();
    public IStartupRegistration Startup { get; } = Substitute.For<IStartupRegistration>();
    public IClipExportService Export { get; } = Substitute.For<IClipExportService>();
    public ICategoryRepository Categories { get; } = Substitute.For<ICategoryRepository>();
    public ICategoryLockService Locks { get; } = Substitute.For<ICategoryLockService>();
    public ILockPasswordService Passwords { get; } = Substitute.For<ILockPasswordService>();
    public IUpdateChecker Updates { get; } = Substitute.For<IUpdateChecker>();
    public IHotkeyRegistration Hotkeys { get; } = Substitute.For<IHotkeyRegistration>();
    public List<Category> StoredCategories { get; } = [];
    public SettingsViewModel ViewModel { get; }
    public AppShellService Shell { get; }
    public SettingsPage Page { get; }
}
