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
    public async Task Quiet_refresh_keeps_existing_rows_when_the_result_is_unchanged()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one"), Item(2, "two") });
        var vm = Create();
        await vm.RefreshAsync();
        var first = vm.Items[0];
        var collectionChanges = 0;
        vm.Items.CollectionChanged += (_, _) => collectionChanges++;

        await vm.RefreshQuietAsync();

        Assert.Equal(0, collectionChanges);
        Assert.Same(first, vm.Items[0]);
    }

    [Fact]
    public async Task Quiet_refresh_sets_the_result_state_without_showing_loading()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one") });
        var vm = Create();
        var loadingObserved = false;
        vm.PropertyChanged += (_, args) =>
            loadingObserved |= args.PropertyName == nameof(vm.State) && vm.State == LoadState.Loading;

        await vm.RefreshQuietAsync();

        Assert.False(loadingObserved);
        Assert.Equal(LoadState.Loaded, vm.State);
    }

    [Fact]
    public async Task Quiet_refresh_inserts_new_rows_without_resetting_the_collection()
    {
        IReadOnlyList<ClipListItem> results = [Item(1, "one"), Item(2, "two")];
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(results));
        var vm = Create();
        await vm.RefreshAsync();
        var first = vm.Items[0];
        var second = vm.Items[1];
        var resetCollection = false;
        vm.Items.CollectionChanged += (_, args) =>
            resetCollection |= args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset;
        results = [Item(3, "new"), Item(1, "one"), Item(2, "two")];

        await vm.RefreshQuietAsync();

        Assert.False(resetCollection);
        Assert.Equal(new long[] { 3, 1, 2 }, vm.Items.Select(item => item.Id));
        Assert.Same(first, vm.Items[1]);
        Assert.Same(second, vm.Items[2]);
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
    public void Clear_confirmation_can_be_cancelled()
    {
        var vm = Create();

        vm.RequestClearCommand.Execute(null);
        Assert.True(vm.IsConfirmingClear);

        vm.CancelClearCommand.Execute(null);
        Assert.False(vm.IsConfirmingClear);
    }

    [Fact]
    public void Reset_transient_state_dismisses_a_pending_clear_confirmation()
    {
        var vm = Create();
        vm.RequestClearCommand.Execute(null);

        vm.ResetTransientState();

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

    [Fact]
    public async Task Clips_are_grouped_into_pinned_and_recent()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "kept", pinned: true), Item(2, "plain") });
        var vm = Create();

        await vm.RefreshAsync();

        Assert.Equal(2, vm.Groups.Count);
        Assert.Equal("kept", Assert.Single(vm.Groups[0]).Text);
        Assert.Equal("plain", Assert.Single(vm.Groups[1]).Text);
        Assert.Equal("2 clips", vm.CountText);
    }

    [Fact]
    public async Task A_single_clip_is_reported_in_the_singular()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "only") });
        var vm = Create();

        await vm.RefreshAsync();

        Assert.Equal("1 clip", vm.CountText);
    }

    private ClipboardPanelViewModel Create() =>
        new(_repository, _clipboard, _dates, NullLogger<ClipboardPanelViewModel>.Instance);

    private static ClipListItem Item(long id, string text, bool pinned = false) =>
        new(id, text, "Notepad", pinned, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", id));
}
