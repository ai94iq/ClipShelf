using ClipShelf.App.Features.Settings;
using ClipShelf.App.Hosting;
using ClipShelf.App.Services;
using ClipShelf.Core.Theming;
using NSubstitute;

namespace ClipShelf.App.Tests.Settings;

public sealed class SettingsViewModelTests
{
    private readonly FakeSettingsStore _store = new();
    private readonly SettingsService _service;
    private readonly IThemeService _theme = Substitute.For<IThemeService>();

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
    public void Loading_does_not_write_settings()
    {
        _ = Create();

        Assert.Null(_store.Saved);
    }

    private SettingsViewModel Create() => new(_service, _theme);
}
