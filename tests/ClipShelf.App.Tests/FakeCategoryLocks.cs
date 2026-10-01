using ClipShelf.App.Services;

namespace ClipShelf.App.Tests;

// In-memory lock service for tests that do not exercise persistence.
internal sealed class FakeCategoryLocks : ICategoryLockService
{
    private readonly HashSet<long> _unlocked = [];

    public IReadOnlyCollection<long> UnlockedCategoryIds => _unlocked;

    public void Unlock(long categoryId) => _unlocked.Add(categoryId);

    public void Lock(long categoryId) => _unlocked.Remove(categoryId);

    public void Reset() => _unlocked.Clear();
}
