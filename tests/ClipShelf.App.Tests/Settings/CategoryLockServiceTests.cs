using ClipShelf.App.Hosting;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using NSubstitute;

namespace ClipShelf.App.Tests.Settings;

public sealed class CategoryLockServiceTests
{
    private readonly FakeSettingsStore _store = new();
    private readonly SettingsService _service;

    public CategoryLockServiceTests() => _service = new SettingsService(_store, new AppSettings());

    [Fact]
    public void Unlocking_is_written_to_settings()
    {
        var locks = New();

        locks.Unlock(7);

        Assert.True(_store.Saved is { UnlockedCategories: [7] });
    }

    [Fact]
    public void Saved_unlocks_are_restored()
    {
        _service.Update(_service.Current with { UnlockedCategories = [7] });

        var locks = New();

        Assert.Contains(7, locks.UnlockedCategoryIds);
    }

    [Fact]
    public void Reset_clears_and_saves_the_empty_list()
    {
        _service.Update(_service.Current with { UnlockedCategories = [7] });
        var locks = New();

        locks.Reset();

        Assert.Empty(locks.UnlockedCategoryIds);
        Assert.True(_store.Saved is { UnlockedCategories: [] });
    }

    [Fact]
    public void Locking_one_category_keeps_the_others()
    {
        _service.Update(_service.Current with { UnlockedCategories = [7, 8] });
        var locks = New();

        locks.Lock(7);

        Assert.True(_store.Saved is { UnlockedCategories: [8] });
    }

    private CategoryLockService New() => new(_service, Substitute.For<IDataChangeNotifier>());
}
