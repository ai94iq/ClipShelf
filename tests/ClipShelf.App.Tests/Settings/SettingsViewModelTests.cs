using ClipShelf.App.Features.Settings;
using ClipShelf.App.Hosting;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;
using NSubstitute;

namespace ClipShelf.App.Tests.Settings;

public sealed class SettingsViewModelTests
{
    private readonly FakeSettingsStore _store = new();
    private readonly SettingsService _service;
    private readonly IThemeService _theme = Substitute.For<IThemeService>();
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly FakeStartupRegistration _startup = new();

    public SettingsViewModelTests() => _service = new SettingsService(_store, new AppSettings());

    [Fact]
    public void Toggling_double_click_off_saves_the_setting()
    {
        var viewModel = Create();

        viewModel.DoubleClickOpensHistory = false;

        Assert.True(_store.Saved is { OpenHistoryOnDoubleClick: false });
    }

    [Fact]
    public void Changing_the_theme_saves_and_applies_it()
    {
        var viewModel = Create();

        viewModel.SelectedTheme = viewModel.ThemeOptions.Single(o => o.Value == AppTheme.Dark);

        Assert.True(_store.Saved is { Theme: AppTheme.Dark });
        _theme.Received(1).Apply(Arg.Is<AppSettings>(s => s.Theme == AppTheme.Dark));
    }

    [Fact]
    public void Changing_the_backdrop_saves_and_applies_it()
    {
        var viewModel = Create();

        viewModel.SelectedBackdrop = viewModel.BackdropOptions.Single(o => o.Value == BackdropKind.Acrylic);

        Assert.True(_store.Saved is { Backdrop: BackdropKind.Acrylic });
        _theme.Received(1).Apply(Arg.Is<AppSettings>(s => s.Backdrop == BackdropKind.Acrylic));
    }

    [Fact]
    public void Changing_the_tray_icon_style_saves_it()
    {
        var viewModel = Create();

        viewModel.SelectedTrayIcon = viewModel.TrayIconOptions.Single(o => o.Value == TrayIconKind.Outline);

        Assert.True(_store.Saved is { TrayIcon: TrayIconKind.Outline });
    }

    [Fact]
    public void Hiding_the_tray_icon_saves_it()
    {
        var viewModel = Create();

        viewModel.ShowTrayIcon = false;

        Assert.True(_store.Saved is { ShowTrayIcon: false });
    }

    [Fact]
    public void Existing_settings_are_preselected()
    {
        var viewModel = Create();

        Assert.Equal(AppTheme.System, viewModel.SelectedTheme.Value);
        Assert.Equal(BackdropKind.Mica, viewModel.SelectedBackdrop.Value);
    }

    [Fact]
    public void Changing_the_hotkey_saves_it()
    {
        var viewModel = Create();

        viewModel.Hotkey = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);

        Assert.True(_store.Saved is
        {
            Hotkey: { Modifiers: HotkeyModifiers.Control | HotkeyModifiers.Alt, VirtualKey: 0x43 },
        });
    }

    [Fact]
    public void The_saved_hotkey_is_shown_when_the_page_loads()
    {
        _service.Update(_service.Current with
        {
            Hotkey = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x41),
        });

        var viewModel = Create();

        Assert.Equal("Ctrl+Shift+A", viewModel.HotkeyDisplay);
    }

    [Fact]
    public void Turning_on_run_at_startup_saves_and_applies_it()
    {
        var viewModel = Create();

        viewModel.RunAtStartup = true;

        Assert.True(_store.Saved is { RunAtStartup: true });
        Assert.Equal(new[] { true }, _startup.Applied);
    }

    [Fact]
    public void Turning_off_run_at_startup_applies_false()
    {
        var viewModel = Create();
        viewModel.RunAtStartup = true;

        viewModel.RunAtStartup = false;

        Assert.True(_store.Saved is { RunAtStartup: false });
        Assert.Equal(new[] { true, false }, _startup.Applied);
    }

    [Fact]
    public async Task Changing_max_items_saves_it_and_prunes_the_history()
    {
        var viewModel = Create();

        viewModel.MaxItems = 250;
        await Task.Delay(600, TestContext.Current.CancellationToken);

        Assert.True(_store.Saved is { MaxItems: 250 });
        await _repository.Received(1).PruneAsync(250, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rapid_max_item_changes_prune_once_with_the_final_value()
    {
        var viewModel = Create();

        viewModel.MaxItems = 50;
        viewModel.MaxItems = 100;
        viewModel.MaxItems = 250;
        await Task.Delay(600, TestContext.Current.CancellationToken);

        await _repository.Received(1).PruneAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).PruneAsync(250, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Loading_does_not_write_settings()
    {
        _ = Create();

        Assert.Null(_store.Saved);
        Assert.Empty(_startup.Applied);
    }

    [Fact]
    public void An_empty_filter_shows_every_settings_group()
    {
        var viewModel = Create();

        Assert.True(viewModel.IsGroupVisible(viewModel.Filter, 0));
        Assert.True(viewModel.IsGroupVisible(viewModel.Filter, 1));
        Assert.True(viewModel.IsGroupVisible(viewModel.Filter, 2));
        Assert.True(viewModel.IsExitVisible(viewModel.Filter));
        Assert.False(viewModel.HasNoMatches(viewModel.Filter));
    }

    [Fact]
    public void Filtering_keeps_only_the_matching_group()
    {
        var viewModel = Create();
        viewModel.Filter = "startup";

        Assert.False(viewModel.IsGroupVisible(viewModel.Filter, 0));
        Assert.False(viewModel.IsGroupVisible(viewModel.Filter, 1));
        Assert.True(viewModel.IsGroupVisible(viewModel.Filter, 2));
        Assert.False(viewModel.IsExitVisible(viewModel.Filter));
        Assert.False(viewModel.HasNoMatches(viewModel.Filter));
    }

    [Fact]
    public void Filtering_matches_the_description_text_too()
    {
        var viewModel = Create();
        viewModel.Filter = "notification area";

        Assert.True(viewModel.IsGroupVisible(viewModel.Filter, 2));
    }

    [Fact]
    public void A_filter_with_no_matches_hides_everything()
    {
        var viewModel = Create();
        viewModel.Filter = "zzz";

        Assert.True(viewModel.HasNoMatches(viewModel.Filter));
    }

    [Fact]
    public void Changing_the_retention_saves_it_and_prunes_older_clips()
    {
        var viewModel = Create();

        viewModel.SelectedRetention = viewModel.RetentionOptions.Single(o => o.Value == 7);

        Assert.True(_store.Saved is { RetentionDays: 7 });
        _repository.Received(1).PruneOlderThanAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Choosing_forever_never_prunes_by_age()
    {
        var viewModel = Create();
        viewModel.SelectedRetention = viewModel.RetentionOptions.Single(o => o.Value == 7);
        _repository.ClearReceivedCalls();

        viewModel.SelectedRetention = viewModel.RetentionOptions.Single(o => o.Value == 0);

        Assert.True(_store.Saved is { RetentionDays: 0 });
        _repository.DidNotReceive().PruneOlderThanAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void The_saved_retention_is_preselected()
    {
        _service.Update(_service.Current with { RetentionDays = 30 });

        var viewModel = Create();

        Assert.Equal(30, viewModel.SelectedRetention.Value);
    }

    [Fact]
    public void Changing_clear_on_sign_out_saves_it()
    {
        var viewModel = Create();

        viewModel.ClearOnSignOut = true;

        Assert.True(_store.Saved is { ClearOnSignOut: true });
    }

    private SettingsViewModel Create() => new(_service, _theme, _repository, _startup);
}
