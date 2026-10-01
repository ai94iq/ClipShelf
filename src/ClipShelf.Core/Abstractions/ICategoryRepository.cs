using ClipShelf.Core.Models;

namespace ClipShelf.Core.Abstractions;

// Named groups for saved clips. Adding a name that already exists returns that category.
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct);

    Task<Category> GetOrAddAsync(string name, CancellationToken ct);
}
