using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using ClipShelf.Desktop.Views;
using ClipShelf.Presentation.Features.ClipboardPanel;
using ClipShelf.Presentation.Platform;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the flyout: rows render and the empty state shows its title.
[Collection("headless")]
[SupportedOSPlatform("windows")]
public sealed class FlyoutHeadlessTests : IDisposable
{
    private readonly HeadlessApp _app = new();
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ICategoryLockService _locks = Substitute.For<ICategoryLockService>();
    private readonly ILockPasswordService _passwords = Substitute.For<ILockPasswordService>();
    private readonly IClipboardWriter _clipboard = Substitute.For<IClipboardWriter>();
    private readonly IDateFormatter _dates = Substitute.For<IDateFormatter>();
    private FlyoutWindow? _window;

    public FlyoutHeadlessTests()
    {
        _dates.FormatSince(Arg.Any<DateTimeOffset>()).Returns("now");
        _locks.UnlockedCategoryIds.Returns(Array.Empty<long>());
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
    }

    [Fact]
    public async Task Flyout_OpensShowingItems()
    {
        _repository.GetRecentAsync(
                Arg.Any<PageCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>(),
                Arg.Any<long?>(), Arg.Any<IReadOnlyCollection<long>?>())
            .Returns(new List<ClipListItem> { Item(1, "one"), Item(2, "two"), Item(3, "three") });
        _repository.CountAsync(
                Arg.Any<string?>(), Arg.Any<long?>(),
                Arg.Any<IReadOnlyCollection<long>?>(), Arg.Any<CancellationToken>())
            .Returns(3);
        var viewModel = Create();
        await viewModel.RefreshAsync();

        _app.Dispatch(() =>
        {
            _window = new FlyoutWindow(viewModel, Substitute.For<IPasteBack>());
            var list = _window.FindControl<ListBox>("ClipList");
            Assert.NotNull(list);
            var rows = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();
            Assert.Equal(3, rows.Count);
        });
    }

    [Fact]
    public async Task Flyout_SearchFiltersToEmptyState()
    {
        _repository.SearchAsync(
                Arg.Any<string>(), Arg.Any<PageCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>(),
                Arg.Any<long?>(), Arg.Any<IReadOnlyCollection<long>?>())
            .Returns(new List<ClipListItem>());
        _repository.CountAsync(
                Arg.Any<string?>(), Arg.Any<long?>(),
                Arg.Any<IReadOnlyCollection<long>?>(), Arg.Any<CancellationToken>())
            .Returns(0);
        var viewModel = Create();
        viewModel.SearchText = "zz-no-match";
        await viewModel.RefreshAsync();

        // The search box debounces, so let its trailing refresh land before the window binds.
        await Task.Delay(450, TestContext.Current.CancellationToken);

        _app.Dispatch(() =>
        {
            _window = new FlyoutWindow(viewModel, Substitute.For<IPasteBack>());
            var title = _window.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(text => text.Text == viewModel.EmptyTitle);
            Assert.NotNull(title);
            Assert.True(((StackPanel)title.Parent!).IsVisible);
        });
    }

    public void Dispose()
    {
        if (_window is not null)
            _app.Dispatch(_window.Close);
        _app.Dispose();
    }

    private ClipboardPanelViewModel Create() =>
        new(_repository, _categories, _locks, _passwords, _clipboard, _dates,
            NullLogger<ClipboardPanelViewModel>.Instance);

    private static ClipListItem Item(long id, string text) =>
        new(id, text, "Notepad", false,
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", id));
}
