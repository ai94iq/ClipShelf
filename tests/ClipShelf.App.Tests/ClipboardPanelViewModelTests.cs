using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Localization;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using NSubstitute;

namespace ClipShelf.App.Tests;

public sealed class ClipboardPanelViewModelTests
{
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ICategoryLockService _locks = new FakeCategoryLocks();
    private readonly ILockPasswordService _passwords = Substitute.For<ILockPasswordService>();
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

    [Fact]
    public async Task Empty_history_shows_the_no_clips_placeholder()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());
        var vm = Create();

        await vm.RefreshAsync();

        Assert.Equal(LoadState.Empty, vm.State);
        Assert.Equal(Tr.Get("Panel_Empty"), vm.EmptyTitle);
        Assert.Equal(Tr.Get("Panel_EmptyHint"), vm.EmptyHint);
    }

    [Fact]
    public async Task Empty_search_shows_the_no_results_placeholder()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());
        var vm = Create();
        vm.SearchText = "zzz";
        _repository.SearchAsync("zzz", null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());

        await vm.RefreshAsync();

        Assert.Equal(LoadState.Empty, vm.State);
        Assert.Equal(Tr.Get("Panel_NoResults"), vm.EmptyTitle);
        Assert.Equal(Tr.Get("Panel_NoResultsHint"), vm.EmptyHint);
    }

    [Fact]
    public void Typing_a_search_updates_the_placeholder_text()
    {
        var vm = Create();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        vm.SearchText = "mail";

        Assert.Contains(nameof(ClipboardPanelViewModel.EmptyTitle), changed);
        Assert.Contains(nameof(ClipboardPanelViewModel.EmptyHint), changed);
    }

    [Fact]
    public async Task Search_is_disabled_while_the_history_is_empty()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());
        var vm = Create();

        await vm.RefreshAsync();

        Assert.False(vm.CanSearch);
    }

    [Fact]
    public async Task Search_stays_enabled_when_a_query_has_no_results()
    {
        var vm = Create();
        vm.SearchText = "zzz";
        _repository.SearchAsync("zzz", null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());

        await vm.RefreshAsync();

        Assert.True(vm.CanSearch);
    }

    [Fact]
    public async Task Search_enables_when_clips_arrive()
    {
        IReadOnlyList<ClipListItem> results = [];
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(results));
        var vm = Create();
        await vm.RefreshAsync();
        Assert.False(vm.CanSearch);

        results = [Item(1, "one")];
        await vm.RefreshQuietAsync();

        Assert.True(vm.CanSearch);
    }

    [Fact]
    public async Task Copying_selected_clips_joins_them_in_list_order()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(3, "one"), Item(2, "two"), Item(1, "three") });
        var vm = Create();
        await vm.RefreshAsync();
        vm.ToggleSelectionCommand.Execute(null);
        vm.Items[2].IsSelected = true;
        vm.Items[0].IsSelected = true;

        vm.CopySelectedCommand.Execute(null);

        _clipboard.Received(1).WriteText("one\r\nthree");
        Assert.False(vm.IsSelecting);
        Assert.All(vm.Items, item => Assert.False(item.IsSelected));
    }

    [Fact]
    public async Task Clicking_a_row_while_selecting_toggles_it_and_copies_nothing()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();
        vm.ToggleSelectionCommand.Execute(null);

        vm.Activate(vm.Items[0]);

        Assert.True(vm.Items[0].IsSelected);
        _clipboard.DidNotReceive().WriteText(Arg.Any<string>());
    }

    [Fact]
    public async Task The_selection_count_and_copy_availability_follow_the_checked_rows()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello"), Item(2, "world") });
        var vm = Create();
        await vm.RefreshAsync();
        vm.ToggleSelectionCommand.Execute(null);
        Assert.False(vm.CanCopySelection);

        vm.Items[1].IsSelected = true;

        Assert.True(vm.CanCopySelection);
        Assert.Equal("1 selected", vm.FooterText);

        vm.Items[0].IsSelected = true;

        Assert.Equal("2 selected", vm.FooterText);
    }

    [Fact]
    public async Task Leaving_selection_clears_the_checked_rows()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();
        vm.ToggleSelectionCommand.Execute(null);
        vm.Items[0].IsSelected = true;

        vm.ToggleSelectionCommand.Execute(null);

        Assert.False(vm.Items[0].IsSelected);
        Assert.Equal(vm.CountText, vm.FooterText);
    }

    [Fact]
    public async Task Copying_selected_clips_signals_the_window()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();
        vm.ToggleSelectionCommand.Execute(null);
        vm.Items[0].IsSelected = true;
        var signalled = false;
        vm.SelectionCopied += (_, _) => signalled = true;

        vm.CopySelectedCommand.Execute(null);

        Assert.True(signalled);
    }

    [Fact]
    public void Reset_transient_state_leaves_select_mode()
    {
        var vm = Create();
        vm.ToggleSelectionCommand.Execute(null);

        vm.ResetTransientState();

        Assert.False(vm.IsSelecting);
    }

    [Fact]
    public async Task Assigning_a_category_calls_the_repository()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();

        await vm.AssignCategoryAsync(vm.Items[0], 7);

        await _repository.Received(1).AssignCategoryAsync(1, 7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creating_a_category_trims_the_name()
    {
        var vm = Create();

        await vm.GetOrAddCategoryAsync("  Work  ");

        await _categories.Received(1).GetOrAddAsync("Work", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Requesting_the_category_menu_hands_over_the_row()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "hello") });
        var vm = Create();
        await vm.RefreshAsync();
        ClipItemViewModel? requested = null;
        vm.CategoryMenuRequested += item => requested = item;

        vm.RequestCategoryMenu(vm.Items[0]);

        Assert.Same(vm.Items[0], requested);
    }

    [Fact]
    public async Task Loading_the_filter_lists_all_categories_plus_all_clips()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work") });
        var vm = Create();

        await vm.RefreshCategoryFilterAsync();

        Assert.Equal(2, vm.CategoryFilterOptions.Count);
        Assert.Null(vm.CategoryFilterOptions[0].Value);
        Assert.Equal(7, vm.CategoryFilterOptions[1].Value);
        Assert.Equal("Work", vm.CategoryFilterOptions[1].Label);
    }

    [Fact]
    public async Task Picking_a_category_filter_narrows_the_query()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work") });
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7)
            .Returns(new List<ClipListItem> { Item(1, "saved") });
        var vm = Create();
        await vm.RefreshCategoryFilterAsync();

        vm.SelectedCategoryFilter = vm.CategoryFilterOptions[1];
        await vm.RefreshAsync();

        Assert.Equal("saved", Assert.Single(vm.Items).Text);
        await _repository.Received().GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7);
    }

    [Fact]
    public async Task An_empty_filtered_view_explains_itself()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work") });
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7)
            .Returns(new List<ClipListItem>());
        var vm = Create();
        await vm.RefreshCategoryFilterAsync();
        vm.SelectedCategoryFilter = vm.CategoryFilterOptions[1];

        await vm.RefreshAsync();

        Assert.Equal(LoadState.Empty, vm.State);
        Assert.Equal(Tr.Get("Panel_EmptyCategory"), vm.EmptyTitle);
    }

    [Fact]
    public async Task Unlocked_categories_are_asked_for_in_the_query()
    {
        _locks.Unlock(7);
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), null,
                Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(7)))
            .Returns(new List<ClipListItem> { Item(1, "secret") });
        var vm = Create();

        await vm.RefreshAsync();

        Assert.Equal("secret", Assert.Single(vm.Items).Text);
    }

    [Fact]
    public async Task Locked_categories_are_marked_and_explained_in_the_filter()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work", true) });
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7, null)
            .Returns(new List<ClipListItem>());
        var vm = Create();

        await vm.RefreshCategoryFilterAsync();
        Assert.Equal(Tr.Format("History_FilterLocked", "Work"), vm.CategoryFilterOptions[1].Label);

        vm.SelectedCategoryFilter = vm.CategoryFilterOptions[1];
        await vm.RefreshAsync();

        Assert.Equal(LoadState.Empty, vm.State);
        Assert.Equal(Tr.Get("Panel_EmptyLocked"), vm.EmptyTitle);
    }

    [Fact]
    public async Task Unlocking_a_locked_category_shows_its_clips()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work", true) });
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7, null)
            .Returns(new List<ClipListItem>());
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7,
                Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(7)))
            .Returns(new List<ClipListItem> { Item(1, "secret") });
        var vm = Create();
        await vm.RefreshCategoryFilterAsync();
        vm.SelectedCategoryFilter = vm.CategoryFilterOptions[1];

        await vm.UnlockCategoryAsync(7);

        Assert.Contains(7, _locks.UnlockedCategoryIds);
        Assert.Equal("Work", vm.CategoryFilterOptions[1].Label);
        Assert.Equal("secret", Assert.Single(vm.Items).Text);
    }

    [Fact]
    public async Task A_lock_change_marks_the_filter_and_keeps_the_selection()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work", true) });
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7, null)
            .Returns(new List<ClipListItem>());
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7,
                Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(7)))
            .Returns(new List<ClipListItem>());
        var vm = Create();
        _locks.Unlock(7);
        await vm.RefreshCategoryFilterAsync();
        vm.SelectedCategoryFilter = vm.CategoryFilterOptions[1];

        _locks.Reset();                      // what lock-on-minimize does
        await vm.RefreshQuietAsync();

        Assert.Equal(7, vm.SelectedCategoryFilter?.Value);
        Assert.True(vm.IsCategoryFilterLocked);
        Assert.Equal(Tr.Format("History_FilterLocked", "Work"), vm.CategoryFilterOptions[1].Label);
        Assert.Equal(Tr.Get("Panel_EmptyLocked"), vm.EmptyTitle);
    }

    [Fact]
    public async Task Refreshing_the_filter_never_reloads_the_whole_list()
    {
        _categories.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { new(7, "Work", true) });
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>(), 7,
                Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(7)))
            .Returns(new List<ClipListItem> { Item(1, "saved") });
        var vm = Create();
        _locks.Unlock(7);
        await vm.RefreshCategoryFilterAsync();
        vm.SelectedCategoryFilter = vm.CategoryFilterOptions[1];
        await vm.RefreshAsync();

        await vm.RefreshCategoryFilterAsync();

        await _repository.DidNotReceive().GetRecentAsync(
            null, 50, Arg.Any<CancellationToken>(), null, null);
        Assert.Equal(7, vm.SelectedCategoryFilter?.Value);
        Assert.Equal("saved", Assert.Single(vm.Items).Text);
    }

    [Fact]
    public async Task Arrow_navigation_moves_the_selection()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one"), Item(2, "two"), Item(3, "three") });
        var vm = Create();
        await vm.RefreshAsync();

        vm.MoveSelection(1);
        Assert.Equal("one", vm.SelectedItem?.Text);
        vm.MoveSelection(1);
        Assert.Equal("two", vm.SelectedItem?.Text);
        vm.MoveSelection(-1);
        Assert.Equal("one", vm.SelectedItem?.Text);
        vm.MoveSelection(-1);
        Assert.Equal("one", vm.SelectedItem?.Text);
    }

    [Fact]
    public async Task Enter_activates_the_selected_clip()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one"), Item(2, "two") });
        var vm = Create();
        await vm.RefreshAsync();
        var signalled = false;
        vm.ItemActivated += (_, _) => signalled = true;
        vm.MoveSelection(1);
        vm.MoveSelection(1);

        vm.ActivateSelected();

        _clipboard.Received(1).WriteText("two");
        Assert.True(signalled);
    }

    [Fact]
    public async Task Enter_without_a_selection_takes_the_first_clip()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one") });
        var vm = Create();
        await vm.RefreshAsync();

        vm.ActivateSelected();

        _clipboard.Received(1).WriteText("one");
    }

    [Fact]
    public async Task Opening_the_flyout_clears_the_keyboard_selection()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "one") });
        var vm = Create();
        await vm.RefreshAsync();
        vm.MoveSelection(1);
        Assert.NotNull(vm.SelectedItem);

        vm.ResetTransientState();

        Assert.Null(vm.SelectedItem);
    }

    [Fact]
    public async Task Activating_an_image_clip_writes_the_image()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>
            {
                new(1, string.Empty, "Snipping Tool", false,
                    new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", 1), null, null, [1, 2, 3]),
            });
        _repository.GetImageAsync(1, Arg.Any<CancellationToken>()).Returns(new byte[] { 1, 2, 3 });
        var vm = Create();
        await vm.RefreshAsync();
        var signalled = false;
        vm.ItemActivated += (_, _) => signalled = true;

        vm.Activate(vm.Items[0]);

        await _clipboard.Received(1).WriteImageAsync(Arg.Is<byte[]>(bytes => bytes.Length == 3));
        _clipboard.DidNotReceive().WriteText(Arg.Any<string>());
        Assert.True(signalled);
    }

    [Fact]
    public async Task Copying_selected_clips_skips_images()
    {
        _repository.GetRecentAsync(null, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>
            {
                Item(1, "one"),
                new(2, string.Empty, "Snipping Tool", false,
                    new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", 2), null, null, [1, 2, 3]),
            });
        var vm = Create();
        await vm.RefreshAsync();
        vm.ToggleSelectionCommand.Execute(null);
        vm.Items[0].IsSelected = true;
        vm.Items[1].IsSelected = true;

        vm.CopySelectedCommand.Execute(null);

        _clipboard.Received(1).WriteText("one");
    }

    private ClipboardPanelViewModel Create() =>
        new(_repository, _categories, _locks, _passwords, _clipboard, _dates,
            NullLogger<ClipboardPanelViewModel>.Instance);

    private static ClipListItem Item(long id, string text, bool pinned = false) =>
        new(id, text, "Notepad", pinned, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", id));
}
