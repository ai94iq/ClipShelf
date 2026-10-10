using ClipShelf.Presentation.Features.Settings;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Localization;
using ClipShelf.Presentation.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Export;
using ClipShelf.Core.Input;
using ClipShelf.Core.Models;
using NSubstitute;

namespace ClipShelf.Presentation.Tests;

// Failure paths the review checks: a failed export shows the error status instead of throwing,
// and a taken hotkey keeps the previous shortcut working behind the warning.
public sealed class FailedExportTests
{
    private readonly FakeSettingsStore _store = new();
    private readonly SettingsService _service;
    private readonly IThemeService _theme = Substitute.For<IThemeService>();
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly IClipExportService _export = Substitute.For<IClipExportService>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly CategoryLockService _locks;
    private readonly ILockPasswordService _passwords;
    private readonly IUpdateChecker _updates = Substitute.For<IUpdateChecker>();
    private readonly IHotkeyRegistration _hotkeyRegistration = Substitute.For<IHotkeyRegistration>();
    private readonly FakeStartupRegistration _startup = new();

    public FailedExportTests()
    {
        _service = new SettingsService(_store, new AppSettings());
        _passwords = new LockPasswordService(_service);
        _locks = new CategoryLockService(_service, Substitute.For<IDataChangeNotifier>());
        _hotkeyRegistration.IsCurrentHotkeyRegistered.Returns(true);
    }

    [Fact]
    public async Task ExportAsync_UnwritablePath_ShowsError()
    {
        _export.ExportAsync(
                Arg.Any<string>(),
                Arg.Any<ClipExportFormat>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new UnauthorizedAccessException("unwritable path")));
        var viewModel = Create();

        var exception = await Record.ExceptionAsync(() => viewModel.ExportAsync("Z:\\nope\\out.json"));

        Assert.Null(exception);
        Assert.True(viewModel.ShowExportError);
        Assert.Equal(Tr.Get("Settings_ExportFailed"), viewModel.ExportStatus);
    }

    [Fact]
    public void Rebinding_to_a_taken_hotkey_warns_and_keeps_the_previous_shortcut()
    {
        var previous = new HotkeyGesture(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56);
        _hotkeyRegistration.IsCurrentHotkeyRegistered.Returns(true);
        _hotkeyRegistration.RegisteredHotkey.Returns(previous);
        var viewModel = Create();

        // The rebind fails: the registration still reports the previous shortcut as active.
        _hotkeyRegistration.IsCurrentHotkeyRegistered.Returns(false);
        _hotkeyRegistration.RegisteredHotkey.Returns(previous);
        viewModel.Hotkey = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);

        Assert.True(viewModel.HasHotkeyWarning);
        Assert.Equal(previous, _hotkeyRegistration.RegisteredHotkey);
        Assert.Equal(
            Tr.Format("Settings_HotkeyStillUsing", HotkeyText.Format(previous)),
            viewModel.HotkeyConflictDetail);
    }

    private SettingsViewModel Create() =>
        new(_service, _theme, _repository, _startup, _export, _categories, _locks, _passwords, _updates,
            new Lazy<IHotkeyRegistration>(() => _hotkeyRegistration),
            NullLogger<SettingsViewModel>.Instance);
}
