using ClipShelf.App.Features.Settings;
using ClipShelf.App.Hosting;

namespace ClipShelf.App.Tests.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Toggling_double_click_off_saves_the_setting()
    {
        var store = new FakeSettingsStore();
        var service = new SettingsService(store, new AppSettings());
        var viewModel = new SettingsViewModel(service);

        viewModel.DoubleClickOpensHistory = false;

        Assert.True(store.Saved is { OpenHistoryOnDoubleClick: false });
    }
}
