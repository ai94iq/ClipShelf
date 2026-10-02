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
    public async Task Renaming_a_category_updates_it()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var ct = TestContext.Current.CancellationToken;
        var work = await repo.GetOrAddAsync("Work", ct);

        var renamed = await repo.RenameAsync(work.Id, "Projects", ct);

        Assert.True(renamed);
        Assert.Equal("Projects", Assert.Single(await repo.GetCategoriesAsync(ct)).Name);
    }

    [Fact]
    public async Task Renaming_to_a_name_in_use_is_rejected()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var ct = TestContext.Current.CancellationToken;
        var work = await repo.GetOrAddAsync("Work", ct);
        await repo.GetOrAddAsync("Personal", ct);

        var renamed = await repo.RenameAsync(work.Id, "personal", ct);

        Assert.False(renamed);
        Assert.Equal("Work", (await repo.GetCategoriesAsync(ct)).Single(c => c.Id == work.Id).Name);
    }

    [Fact]
    public async Task Deleting_a_category_leaves_its_clips_uncategorized()
    {
        using var db = new TempDatabase();
        var repo = New(db);
        var clips = new ClipRepository(db.Factory, Substitute.For<IDataChangeNotifier>());
        var ct = TestContext.Current.CancellationToken;
        var work = await repo.GetOrAddAsync("Work", ct);
        await clips.AddOrBumpAsync(
            "saved", null, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), ct);
        var clip = (await clips.GetRecentAsync(null, 50, ct))[0];
        await clips.AssignCategoryAsync(clip.Id, work.Id, ct);

        await repo.DeleteAsync(work.Id, ct);

        Assert.Empty(await repo.GetCategoriesAsync(ct));
        var kept = Assert.Single(await clips.GetRecentAsync(null, 50, ct));
        Assert.Null(kept.CategoryId);
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
