using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using NSubstitute;

namespace ClipShelf.App.Tests;

public sealed class ClipboardPanelViewModelTests
{
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly IClipboardWriter _clipboard = Substitute.For<IClipboardWriter>();
    private readonly IDateFormatter _dates = Substitute.For<IDateFormatter>();

    public ClipboardPanelViewModelTests() => _dates.FormatSince(Arg.Any<DateTimeOffset>()).Returns("now");

    [Fact]
    public async Task Loads_recent_clips()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one"), Item(2, "two") });
        var vm = Create();

        await vm.RefreshAsync();

        Assert.Equal(LoadState.Loaded, vm.State);
        Assert.Equal(2, vm.Items.Count);
        Assert.Equal("one", vm.Items[0].Text);
    }

    [Fact]
    public async Task No_clips_gives_empty_state()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());
        var vm = Create();

        await vm.RefreshAsync();

        Assert.Equal(LoadState.Empty, vm.State);
    }

    [Fact]
    public async Task Search_uses_the_search_query()
    {
        _repository.SearchAsync("mail", null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "email") });
        var vm = Create();
        vm.SearchText = "mail";

        await vm.RefreshAsync();

        await _repository.Received(1).SearchAsync("mail", null, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activating_an_item_copies_it_and_signals_the_window()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();
        var signalled = false;
        vm.ItemActivated += (_, _) => signalled = true;

        vm.Activate(vm.Items[0]);

        _clipboard.Received(1).WriteText("hello");
        Assert.True(signalled);
    }

    [Fact]
    public async Task Toggling_pin_flips_the_stored_flag()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();

        await vm.TogglePinAsync(vm.Items[0]);

        await _repository.Received(1).SetPinnedAsync(1, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleting_an_item_calls_the_repository()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();

        await vm.DeleteAsync(vm.Items[0]);

        await _repository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Clear_all_is_confirmed_first()
    {
        var vm = Create();

        vm.RequestClearCommand.Execute(null);
        Assert.True(vm.IsConfirmingClear);

        vm.CancelClearCommand.Execute(null);
        Assert.False(vm.IsConfirmingClear);
    }

    [Fact]
    public async Task Confirming_clear_removes_unpinned_clips()
    {
        var vm = Create();

        await vm.ConfirmClearCommand.ExecuteAsync(null);

        await _repository.Received(1).ClearUnpinnedAsync(Arg.Any<CancellationToken>());
        Assert.False(vm.IsConfirmingClear);
    }

    private ClipboardPanelViewModel Create() =>
        new(_repository, _clipboard, _dates, NullLogger<ClipboardPanelViewModel>.Instance);

    private static ClipListItem Item(long id, string text, bool pinned = false) =>
        new(id, text, "Notepad", pinned, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", id));
}
