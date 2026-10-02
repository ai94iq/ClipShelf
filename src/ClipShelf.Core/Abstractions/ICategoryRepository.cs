using ClipShelf.Core.Models;

namespace ClipShelf.Core.Abstractions;

// Named groups for saved clips. Adding a name that already exists returns that category.
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct);

    Task<Category> GetOrAddAsync(string name, CancellationToken ct);

    // Marks a category locked (its clips are hidden) or clears that mark.
    Task SetLockedAsync(long id, bool locked, CancellationToken ct);

    // Renames a category; false when another category already uses that name.
    Task<bool> RenameAsync(long id, string name, CancellationToken ct);

    // Deletes a category; its clips stay in the history without a category.
    Task DeleteAsync(long id, CancellationToken ct);

    // Used when the shared password is removed, so no category is left unopenable.
    Task ClearLocksAsync(CancellationToken ct);
}
