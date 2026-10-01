using ClipShelf.Core.Models;

namespace ClipShelf.Core.Abstractions;

// Named groups for saved clips. Adding a name that already exists returns that category.
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct);

    Task<Category> GetOrAddAsync(string name, CancellationToken ct);

    // Marks a category locked (its clips are hidden) or clears that mark.
    Task SetLockedAsync(long id, bool locked, CancellationToken ct);

    // Used when the shared password is removed, so no category is left unopenable.
    Task ClearLocksAsync(CancellationToken ct);
}
