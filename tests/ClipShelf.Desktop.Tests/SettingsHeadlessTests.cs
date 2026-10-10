using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Input;
using ClipShelf.Core.Models;
using ClipShelf.Desktop.Controls;
using ClipShelf.Desktop.Shell;
using ClipShelf.Desktop.Views;
using ClipShelf.Presentation.Features.Settings;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Localization;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the settings page: all eight groups render, and a taken hotkey
// keeps the previous shortcut working behind the warning (Review Focus 3, VM level).
[Collection("headless")]
[SupportedOSPlatform("windows")]
public sealed class SettingsHeadlessTests : IDisposable
{
    private readonly HeadlessApp _app = new();
    private readonly FakeSettings _store = new();
    private readonly SettingsService _service;
    private readonly IThemeService _theme = Substitute.For<IThemeService>();
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly IStartupRegistration _startup = Substitute.For<IStartupRegistration>();
    private readonly IClipExportService _export = Substitute.For<IClipExportService>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly CategoryLockService _locks;
    private readonly ILockPasswordService _passwords;
    private readonly IUpdateChecker _updates = Substitute.For<IUpdateChecker>();
    private readonly IHotkeyRegistration _hotkeyRegistration = Substitute.For<IHotkeyRegistration>();
    private Window? _host;

    public SettingsHeadlessTests()
    {
        _service = new SettingsService(_store, new AppSettings());
        _passwords = new LockPasswordService(_service);
        _locks = new CategoryLockService(_service, Substitute.For<IDataChangeNotifier>());
        _hotkeyRegistration.IsCurrentHotkeyRegistered.Returns(true);
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
    }

    [Fact]
    public async Task Settings_LoadsAllGroups()
    {
        var viewModel = Create();
        await viewModel.RefreshCategoriesAsync();

        _app.Dispatch(() =>
        {
            var page = new SettingsPage(viewModel, new Lazy<TrayController>(() => null!));
            _host = new Window { Content = page };
            _host.Show();

            var labels = _host.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(text => text.Text)
                .Concat(_host.GetVisualDescendants().OfType<SettingsCard>().Select(card => card.Header))
                .ToList();
            Assert.Contains(Tr.Get("Settings_Appearance"), labels);
            Assert.Contains(Tr.Get("Settings_Tray"), labels);
            Assert.Contains(Tr.Get("Settings_Behavior"), labels);
            Assert.Contains(Tr.Get("Settings_History"), labels);
            Assert.Contains(Tr.Get("Common_Export"), labels);
            Assert.Contains(Tr.Get("Settings_Categories"), labels);
            Assert.Contains(Tr.Get("Settings_Updates"), labels);
            Assert.Contains(Tr.Get("Settings_ExitApp"), labels);
        });
    }

    [Fact]
    public void Settings_TakenHotkey_KeepsPrevious()
    {
        var previous = new HotkeyGesture(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56);
        _hotkeyRegistration.RegisteredHotkey.Returns(previous);
        var viewModel = Create();

        // The rebind fails: the registration still reports the previous shortcut as active.
        _hotkeyRegistration.IsCurrentHotkeyRegistered.Returns(false);
        viewModel.Hotkey = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);

        Assert.True(viewModel.HasHotkeyWarning);
        Assert.Equal(previous, _hotkeyRegistration.RegisteredHotkey);
        Assert.Equal(
            Tr.Format("Settings_HotkeyStillUsing", HotkeyText.Format(previous)),
            viewModel.HotkeyConflictDetail);
    }

    public void Dispose()
    {
        if (_host is not null)
            _app.Dispatch(_host.Close);
        _app.Dispose();
    }

    private SettingsViewModel Create() =>
        new(_service, _theme, _repository, _startup, _export, _categories, _locks, _passwords, _updates,
            new Lazy<IHotkeyRegistration>(() => _hotkeyRegistration),
            NullLogger<SettingsViewModel>.Instance);

    private sealed class FakeSettings : ISettingsStore
    {
        public AppSettings Load() => new();

        public void Save(AppSettings settings)
        {
        }
    }
}
