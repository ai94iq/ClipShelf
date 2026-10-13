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
    private readonly AppLifetime _lifetime = new();
    private readonly ManualTime _time = new();
    private MainWindow? _window;
    private FlyoutTestKit? _flyout;

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

    [Fact]
    public Task Exit_marks_the_app_as_exiting_before_it_asks_to_exit() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings());
        var exitingWhenAsked = false;
        shell.ExitRequested += (_, _) => exitingWhenAsked = _lifetime.IsExiting;

        shell.Exit();

        Assert.True(exitingWhenAsked);
    });

    [Fact]
    public Task The_tray_history_item_opens_the_window_on_history() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings());

        shell.ShowHistory();

        Assert.True(_window!.IsVisible);
        Assert.False(_window.IsSettings);
    });

    [Fact]
    public Task The_tray_settings_item_opens_the_window_on_settings() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings());

        shell.ShowSettings();

        Assert.True(_window!.IsSettings);
    });

    [Fact]
    public Task The_hotkey_toggles_the_flyout() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings());

        shell.TogglePanel();
        Assert.True(_flyout!.Window.IsOpen);

        shell.TogglePanel();
        Assert.False(_flyout.Window.IsOpen);
    });

    [Fact]
    public Task Open_full_history_closes_the_flyout_and_opens_history() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings());
        shell.TogglePanel();

        _flyout!.ViewModel.OpenHistoryCommand.Execute(null);

        Assert.False(_flyout.Window.IsOpen);
        Assert.True(_window!.IsVisible);
        Assert.False(_window.IsSettings);
    });

    [Fact]
    public Task A_tray_click_toggles_the_flyout() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings());

        shell.OnTrayClicked();
        Assert.True(_flyout!.Window.IsOpen);

        _time.Advance(TimeSpan.FromSeconds(1));
        shell.OnTrayClicked();
        Assert.False(_flyout.Window.IsOpen);
    });

    [Fact]
    public Task A_tray_double_click_opens_history() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings { OpenHistoryOnDoubleClick = true });

        shell.OnTrayClicked();
        _time.Advance(TimeSpan.FromMilliseconds(200));
        shell.OnTrayClicked();

        Assert.False(_flyout!.Window.IsOpen);
        Assert.True(_window!.IsVisible);
        Assert.False(_window.IsSettings);
    });

    [Fact]
    public Task With_double_click_off_the_second_click_just_toggles() => Ui.Run(() =>
    {
        var shell = Create(new AppSettings { OpenHistoryOnDoubleClick = false });

        shell.OnTrayClicked();
        _time.Advance(TimeSpan.FromMilliseconds(200));
        shell.OnTrayClicked();

        Assert.False(_flyout!.Window.IsOpen);
        Assert.Null(_window);
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task Minimizing_the_window_locks_categories_when_asked(bool lockOnMinimize) => Ui.Run(() =>
    {
        var shell = Create(new AppSettings { LockOnMinimize = lockOnMinimize });
        shell.ShowHistory();

        _window!.WindowState = Avalonia.Controls.WindowState.Minimized;

        _locks.Received(lockOnMinimize ? 1 : 0).Reset();
    });

    private AppShellService Create(AppSettings settings) =>
        Create(new SettingsService(Substitute.For<ISettingsStore>(), settings));

    private AppShellService Create(SettingsService settings) =>
        new(_hotkey, settings, Substitute.For<ISystemTheme>(), _locks, _lifetime,
            new Lazy<MainWindow>(() => _window = new MainWindow(_lifetime)),
            new Lazy<FlyoutWindow>(() => (_flyout = new FlyoutTestKit()).Window),
            NullLogger<AppShellService>.Instance, _time);
}
