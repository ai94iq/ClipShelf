using ClipShelf.Core.Abstractions;

namespace ClipShelf.App.Services;

// Remembers which locked categories are unlocked and writes the list to settings, so it can
// survive a restart unless a lock-on event clears it.
public sealed class CategoryLockService : ICategoryLockService
{
    private readonly SettingsService _settings;
    private readonly IDataChangeNotifier _changes;
    private readonly HashSet<long> _unlocked;

    public CategoryLockService(SettingsService settings, IDataChangeNotifier changes)
    {
        _settings = settings;
        _changes = changes;
        _unlocked = [.. settings.Current.UnlockedCategories];
    }

    public IReadOnlyCollection<long> UnlockedCategoryIds => _unlocked;

    public void Unlock(long categoryId)
    {
        if (!_unlocked.Add(categoryId)) return;

        Persist();
        _changes.Notify(new DataChanged("lock"));
    }

    public void Lock(long categoryId)
    {
        if (!_unlocked.Remove(categoryId)) return;

        Persist();
        _changes.Notify(new DataChanged("lock"));
    }

    public void Reset()
    {
        if (_unlocked.Count == 0) return;

        _unlocked.Clear();
        Persist();
        _changes.Notify(new DataChanged("lock"));
    }

    private void Persist() =>
        _settings.Update(_settings.Current with { UnlockedCategories = [.. _unlocked] });
}
