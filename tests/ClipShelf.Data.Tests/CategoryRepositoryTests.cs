using ClipShelf.Core.Abstractions;
using ClipShelf.Data.Repositories;
using NSubstitute;

namespace ClipShelf.Data.Tests;

public sealed class CategoryRepositoryTests
{
    private static CategoryRepository New(TempDatabase db, IDataChangeNotifier? changes = null) =>
        new(db.Factory, changes ?? Substitute.For<IDataChangeNotifier>());

    [Fact]
    public async Task Adding_a_category_returns_it_from_the_list()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var ct = TestContext.Current.CancellationToken;

        var added = await repo.GetOrAddAsync("Work", ct);
        var categories = await repo.GetCategoriesAsync(ct);

        Assert.True(added.Id > 0);
        Assert.Equal("Work", Assert.Single(categories).Name);
    }

    [Fact]
    public async Task An_existing_name_returns_the_same_category_ignoring_case()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var ct = TestContext.Current.CancellationToken;

        var first = await repo.GetOrAddAsync("Work", ct);
        var again = await repo.GetOrAddAsync("work", ct);

        Assert.Equal(first.Id, again.Id);
        Assert.Single(await repo.GetCategoriesAsync(ct));
    }

    [Fact]
    public async Task Locking_a_category_marks_it()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var ct = TestContext.Current.CancellationToken;
        var category = await repo.GetOrAddAsync("Work", ct);

        await repo.SetLockedAsync(category.Id, true, ct);

        Assert.True(Assert.Single(await repo.GetCategoriesAsync(ct)).IsLocked);
    }

    [Fact]
    public async Task Clearing_all_locks_unlocks_every_category()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var ct = TestContext.Current.CancellationToken;
        var work = await repo.GetOrAddAsync("Work", ct);
        var personal = await repo.GetOrAddAsync("Personal", ct);
        await repo.SetLockedAsync(work.Id, true, ct);
        await repo.SetLockedAsync(personal.Id, true, ct);

        await repo.ClearLocksAsync(ct);

        Assert.All(await repo.GetCategoriesAsync(ct), category => Assert.False(category.IsLocked));
    }

    [Fact]
    public async Task Adding_a_category_notifies_open_views()
    {
        using var db = new TempDatabase();
        var changes = Substitute.For<IDataChangeNotifier>();
        var repo = New(db, changes);
        var ct = TestContext.Current.CancellationToken;

        await repo.GetOrAddAsync("Work", ct);

        changes.Received(1).Notify(Arg.Is<DataChanged>(c => c.Area == "category"));
    }
}
