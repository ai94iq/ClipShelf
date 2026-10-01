using ClipShelf.App.Hosting;

namespace ClipShelf.App.Tests.Settings;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Update_persists_and_exposes_the_new_settings()
    {
        var store = new FakeSettingsStore();
        var service = new SettingsService(store, new AppSettings());

        service.Update(service.Current with { MaxItems = 42 });

        Assert.Equal(42, service.Current.MaxItems);
        Assert.Equal(42, store.Saved?.MaxItems);
    }

    [Fact]
    public void Update_raises_changed()
    {
        var service = new SettingsService(new FakeSettingsStore(), new AppSettings());
        var raised = 0;
        service.Changed += (_, _) => raised++;

        service.Update(service.Current with { MaxItems = 10 });

        Assert.Equal(1, raised);
    }
}
