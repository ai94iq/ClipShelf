namespace ClipShelf.App.Services;

// Remembers which locked categories were unlocked in this run of the app; nothing is written to disk.
public interface ICategoryLockService
{
    IReadOnlyCollection<long> UnlockedCategoryIds { get; }

    void Unlock(long categoryId);

    void Lock(long categoryId);

    // Forgets every session unlock; used when the password is removed.
    void Reset();
}
