using Avalonia.Controls;
using Avalonia.VisualTree;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using ClipShelf.Desktop.Views;
using ClipShelf.Presentation.Features.ClipboardPanel;
using ClipShelf.Presentation.Localization;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the history page: grouped headers render, and picking a locked
// category reverts the filter when the password dialog is cancelled (Review Focus 1).
[Collection("headless")]
public sealed class HistoryHeadlessTests : IDisposable
{
    private readonly HeadlessApp _app = new();
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ICategoryLockService _locks = Substitute.For<ICategoryLockService>();
    private readonly ILockPasswordService _passwords = Substitute.For<ILockPasswordService>();
    private readonly IClipboardWriter _clipboard = Substitute.For<IClipboardWriter>();
    private readonly IDateFormatter _dates = Substitute.For<IDateFormatter>();
    private Window? _host;

    public HistoryHeadlessTests()
    {
        _dates.FormatSince(Arg.Any<DateTimeOffset>()).Returns("now");
        _locks.UnlockedCategoryIds.Returns(Array.Empty<long>());
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
        _repository.GetRecentAsync(
                Arg.Any<PageCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>(),
                Arg.Any<long?>(), Arg.Any<IReadOnlyCollection<long>?>())
            .Returns(new List<ClipListItem>());
        _repository.CountAsync(
                Arg.Any<string?>(), Arg.Any<long?>(),
                Arg.Any<IReadOnlyCollection<long>?>(), Arg.Any<CancellationToken>())
            .Returns(0);
    }

    [Fact]
    public async Task History_RendersGroupHeaders()
    {
        _repository.GetRecentAsync(
                Arg.Any<PageCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>(),
                Arg.Any<long?>(), Arg.Any<IReadOnlyCollection<long>?>())
            .Returns(new List<ClipListItem>
            {
                Item(1, "pinned one", true), Item(2, "recent one"), Item(3, "recent two"),
            });
        _repository.CountAsync(
                Arg.Any<string?>(), Arg.Any<long?>(),
                Arg.Any<IReadOnlyCollection<long>?>(), Arg.Any<CancellationToken>())
            .Returns(3);
        var viewModel = Create();
        await viewModel.RefreshAsync();

        _app.Dispatch(() =>
        {
            var page = new HistoryPage(viewModel) { IsActive = true };
            _host = new Window { Content = page };
            _host.Show();

            var texts = _host.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(text => text.Text)
                .ToList();
            Assert.Contains(Tr.Get("History_Pinned"), texts);
            Assert.Contains(Tr.Get("History_Recent"), texts);
        });
    }

    [Fact]
    public async Task History_LockedFilterCancel_Reverts()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Secrets", true) });
        var viewModel = Create();
        await viewModel.RefreshCategoryFilterAsync();
        await viewModel.RefreshAsync();
        var all = viewModel.CategoryFilterOptions[0];
        var locked = viewModel.CategoryFilterOptions.First(option => option.Value == 7);
        Assert.Equal(all, viewModel.SelectedCategoryFilter);

        ComboBox? filter = null;
        _app.Dispatch(() =>
        {
            // No host window: with no owner the password prompt cannot open, which is exactly
            // the cancel path, and the selection must revert.
            var page = new HistoryPage(viewModel) { IsActive = true };
            filter = page.FindControl<ComboBox>("CategoryFilter");
            Assert.NotNull(filter);
            Assert.Equal(2, filter.ItemCount);
            viewModel.SelectedCategoryFilter = locked;
        });

        await WaitForAsync(() => Equals(viewModel.SelectedCategoryFilter, all));
        Assert.Equal(all, viewModel.SelectedCategoryFilter);
    }

    public void Dispose()
    {
        if (_host is not null)
            _app.Dispatch(_host.Close);
        _app.Dispose();
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            while (!condition())
                await Task.Delay(50, timeout.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("The category selection never reverted after cancelling the dialog.");
            throw;
        }
    }

    private ClipboardPanelViewModel Create() =>
        new(_repository, _categories, _locks, _passwords, _clipboard, _dates,
            NullLogger<ClipboardPanelViewModel>.Instance);

    private static ClipListItem Item(long id, string text, bool pinned = false) =>
        new(id, text, "Notepad", pinned,
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", id));
}
