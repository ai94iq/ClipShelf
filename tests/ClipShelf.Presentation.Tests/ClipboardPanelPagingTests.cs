using ClipShelf.Presentation.Features.ClipboardPanel;
using ClipShelf.Presentation.Localization;
using ClipShelf.Presentation.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using NSubstitute;

namespace ClipShelf.Presentation.Tests;

// Paging at the view-model level: scrolling near the end loads the next page, the list grows,
// and the total stays truthful (Review Focus: incremental history with true totals).
public sealed class ClipboardPanelPagingTests
{
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ICategoryLockService _locks = new FakeCategoryLocks();
    private readonly ILockPasswordService _passwords = Substitute.For<ILockPasswordService>();
    private readonly IClipboardWriter _clipboard = Substitute.For<IClipboardWriter>();
    private readonly IDateFormatter _dates = Substitute.For<IDateFormatter>();

    public ClipboardPanelPagingTests() => _dates.FormatSince(Arg.Any<DateTimeOffset>()).Returns("now");

    [Fact]
    public async Task Paging_LoadMore_GrowsItems()
    {
        var first = Enumerable.Range(1, 50).Select(id => Item(id, $"item {id}")).ToList();
        var second = Enumerable.Range(51, 3).Select(id => Item(id, $"item {id}")).ToList();
        _repository.GetRecentAsync(
                Arg.Any<PageCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>(),
                Arg.Any<long?>(), Arg.Any<IReadOnlyCollection<long>?>())
            .Returns(call => Task.FromResult<IReadOnlyList<ClipListItem>>(
                call.Arg<PageCursor?>() is null ? first : second));
        _repository.CountAsync(
                Arg.Any<string?>(), Arg.Any<long?>(),
                Arg.Any<IReadOnlyCollection<long>?>(), Arg.Any<CancellationToken>())
            .Returns(53);
        var viewModel = Create();

        await viewModel.RefreshAsync();

        Assert.Equal(50, viewModel.Items.Count);
        Assert.True(viewModel.HasMore);

        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(53, viewModel.Items.Count);
        Assert.Equal(53, viewModel.TotalCount);
        Assert.Equal(Tr.Plural("History_Count", 53), viewModel.CountText);
    }

    private ClipboardPanelViewModel Create() =>
        new(_repository, _categories, _locks, _passwords, _clipboard, _dates,
            NullLogger<ClipboardPanelViewModel>.Instance);

    private static ClipListItem Item(long id, string text) =>
        new(id, text, "Notepad", false,
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", id));
}
