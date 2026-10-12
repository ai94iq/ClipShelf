using ClipShelf.Core.Input;
using ClipShelf.Desktop.Shell;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Platform;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

public sealed class AppShellServiceTests
{
    private readonly ICategoryLockService _locks = Substitute.For<ICategoryLockService>();
    private readonly IGlobalHotkey _hotkey = Substitute.For<IGlobalHotkey>();

    [Fact]
    public void Exit_resets_the_locks_before_it_asks_the_app_to_exit()
    {
        var order = new List<string>();
        _locks.When(l => l.Reset()).Do(_ => order.Add("reset"));
        var shell = Create(new AppSettings { LockOnExit = true });
        shell.ExitRequested += (_, _) => order.Add("exit");

        shell.Exit();

        Assert.Equal(["reset", "exit"], order);
    }

    [Fact]
    public void Exit_keeps_the_locks_when_lock_on_exit_is_off()
    {
        var shell = Create(new AppSettings { LockOnExit = false });

        shell.Exit();

        _locks.DidNotReceive().Reset();
    }

    [Fact]
    public void The_saved_hotkey_is_registered_at_startup()
    {
        var settings = new AppSettings();
        _hotkey.Register(settings.Hotkey).Returns(true);

        var shell = Create(settings);

        Assert.True(shell.IsCurrentHotkeyRegistered);
    }

    [Fact]
    public void A_taken_hotkey_keeps_the_previous_one_active()
    {
        var settings = new AppSettings();
        _hotkey.Register(settings.Hotkey).Returns(true);
        var store = Substitute.For<ISettingsStore>();
        var service = new SettingsService(store, settings);
        var shell = Create(service);
        var taken = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x51);
        _hotkey.Register(taken).Returns(false);

        service.Update(settings with { Hotkey = taken });

        Assert.False(shell.IsCurrentHotkeyRegistered);
        Assert.Equal(settings.Hotkey, shell.RegisteredHotkey);
    }

    private AppShellService Create(AppSettings settings) =>
        Create(new SettingsService(Substitute.For<ISettingsStore>(), settings));

    private AppShellService Create(SettingsService settings) =>
        new(_hotkey, settings, Substitute.For<ISystemTheme>(), _locks, new AppLifetime(),
            NullLogger<AppShellService>.Instance);
}
