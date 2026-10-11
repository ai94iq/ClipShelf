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

// Headless coverage for the Linux flyout twin: rows render from the shared
// template. CA1416 is scoped: constructing the window never touches X11.
[Collection("headless")]
public sealed class LinuxFlyoutHeadlessTests : IDisposable
{
    private readonly HeadlessApp _app = new();
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ICategoryLockService _locks = Substitute.For<ICategoryLockService>();
    private readonly ILockPasswordService _passwords = Substitute.For<ILockPasswordService>();
    private readonly IClipboardWriter _clipboard = Substitute.For<IClipboardWriter>();
    private readonly IDateFormatter _dates = Substitute.For<IDateFormatter>();
    private LinuxFlyoutWindow? _window;

    public LinuxFlyoutHeadlessTests()
    {
        _dates.FormatSince(Arg.Any<DateTimeOffset>()).Returns("now");
        _locks.UnlockedCategoryIds.Returns(Array.Empty<long>());
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
    }

    [Fact]
    public async Task LinuxFlyout_OpensShowingItems()
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

#pragma warning disable CA1416 // Window construction parses XAML only; X11 starts on show paths.
        _app.Dispatch(() =>
        {
            _window = new LinuxFlyoutWindow(viewModel, Substitute.For<IPasteBack>());
            var list = _window.FindControl<ListBox>("ClipList");
            Assert.NotNull(list);
            var rows = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();
            Assert.Equal(3, rows.Count);
        });
#pragma warning restore CA1416
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
