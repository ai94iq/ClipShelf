using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;

namespace ClipShelf.Data.Repositories;

// SQLite + Dapper for the category list; clips are assigned through the clip repository.
public sealed class CategoryRepository(SqliteConnectionFactory factory, IDataChangeNotifier changes)
    : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct) =>
        Task.Run<IReadOnlyList<Category>>(() =>
        {
            using var connection = factory.Open();
            return connection.Query<CategoryRow>("SELECT Id, Name FROM Category ORDER BY Name COLLATE NOCASE;")
                .Select(row => new Category(row.Id, row.Name))
                .ToList();
        }, ct);

    public Task<Category> GetOrAddAsync(string name, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            var existing = connection.Query<CategoryRow>(
                "SELECT Id, Name FROM Category WHERE Name = @name COLLATE NOCASE;", new { name }).FirstOrDefault();
            if (existing is not null) return new Category(existing.Id, existing.Name);

            var id = connection.ExecuteScalar<long>(
                "INSERT INTO Category (Name) VALUES (@name) RETURNING Id;", new { name });
            changes.Notify(new DataChanged("category"));
            return new Category(id, name);
        }, ct);

    private sealed record CategoryRow(long Id, string Name);
}
