using System.Collections.ObjectModel;
using ClipShelf.Core.Abstractions;

namespace ClipShelf.App.Features.ClipboardPanel;

// Drives the floating history list: load, search, copy, pin, delete and clear.
public sealed partial class ClipboardPanelViewModel : PageViewModel, IIncrementalSource
{
    private const int PageSize = 50;
    private const int SearchDebounceMs = 300;

    private readonly IClipRepository _repository;
    private readonly ICategoryRepository _categories;
    private readonly ICategoryLockService _locks;
    private readonly ILockPasswordService _passwords;
    private readonly IClipboardWriter _clipboard;
    private readonly IDateFormatter _dates;
    private readonly HashSet<long> _lockedCategoryIds = [];
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _noticeCts;
    private readonly List<PendingDelete> _pendingDeletes = [];
    private bool _quietRefreshInProgress;
    private bool _quietRefreshRequested;
    private PageCursor? _cursor;
    private int _loadGeneration;

    public ClipboardPanelViewModel(
        IClipRepository repository,
        ICategoryRepository categories,
        ICategoryLockService locks,
        ILockPasswordService passwords,
        IClipboardWriter clipboard,
        IDateFormatter dates,
        ILogger<ClipboardPanelViewModel> log) : base(log)
    {
        _repository = repository;
        _categories = categories;
        _locks = locks;
        _passwords = passwords;
        _clipboard = clipboard;
        _dates = dates;
        ReloadQuietlyOnChange("clip", "lock");
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(CanSearch));
            OnPropertyChanged(nameof(FooterText));
        };
    }

    public ObservableCollection<ClipItemViewModel> Items { get; } = [];

    // Grouped view (Pinned, then Recent) used by the full history page.
    public ObservableCollection<ClipGroup> Groups { get; } = [];

    // The bottom row on screen: the end of the last group, which is what the views watch to load
    // the next page. Items' raw order is recency order, not the display order.
    public ClipItemViewModel? LastVisibleItem => Groups.Count == 0 ? null : Groups[^1].LastOrDefault();

    public bool HasItems => Items.Count > 0;

    // The total the current query can show, not just the loaded page, so the footer tells the truth.
    public string CountText => Tr.Plural("History_Count", TotalCount);

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    // Paging: the view asks for the next page near the end of the scroll (IIncrementalSource).
    [ObservableProperty]
    public partial bool HasMore { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    // The full-history category filter: "All categories" plus one entry per category.
    public ObservableCollection<Option<long?>> CategoryFilterOptions { get; } = [];

    [ObservableProperty]
    public partial Option<long?>? SelectedCategoryFilter { get; set; }

    private bool _updatingFilter;

    public bool IsCategoryFilterActive => SelectedCategoryFilter?.Value is not null;

    public bool IsCategoryFilterLocked =>
        SelectedCategoryFilter?.Value is long id && _lockedCategoryIds.Contains(id);

    public async Task RefreshCategoryFilterAsync()
    {
        var categories = await _categories.GetCategoriesAsync(CancellationToken.None);
        var previous = SelectedCategoryFilter?.Value;

        // Rebuilding the list resets the combo's selection for a moment; guard the whole rebuild
        // so that never triggers a refresh without the filter.
        _updatingFilter = true;
        try
        {
            _lockedCategoryIds.Clear();
            CategoryFilterOptions.Clear();
            CategoryFilterOptions.Add(new Option<long?>(null, Tr.Get("History_FilterAll")));
            foreach (var category in categories)
            {
                var locked = category.IsLocked && !_locks.UnlockedCategoryIds.Contains(category.Id);
                if (locked) _lockedCategoryIds.Add(category.Id);
                CategoryFilterOptions.Add(new Option<long?>(
                    category.Id, locked ? Tr.Format("History_FilterLocked", category.Name) : category.Name));
            }

            var match = CategoryFilterOptions.FirstOrDefault(option => option.Value == previous)
                ?? CategoryFilterOptions[0];
            if (!Equals(match, SelectedCategoryFilter)) SelectedCategoryFilter = match;
        }
        finally
        {
            _updatingFilter = false;
        }
    }

    public async Task UnlockCategoryAsync(long id)
    {
        _locks.Unlock(id);
        await RefreshCategoryFilterAsync();
        await RefreshAsync();
    }

    public bool VerifyLockPassword(string password) => _passwords.Verify(password);

    partial void OnSelectedCategoryFilterChanged(Option<long?>? value)
    {
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyHint));
        if (_updatingFilter) return;

        _ = RefreshAsync();
    }

    // Multi-select: rows are checked, then copied together in one go.
    public int SelectedCount => Items.Count(item => item.IsSelected);

    public string SelectionCountText => Tr.Format("Selection_Count", SelectedCount);

    public bool CanCopySelection => SelectedCount > 0;

    // While a copy confirmation is showing, the footer shows it instead of the count.
    public string FooterText => IsSelecting ? SelectionCountText : CopyNotice ?? CountText;

    public bool ShowSelectButton => !IsSelecting;

    // Placeholder copy: an empty history, an empty search and an empty filter read differently.
    public string EmptyTitle =>
        IsSearchActive ? Tr.Get("Panel_NoResults")
        : IsCategoryFilterLocked ? Tr.Get("Panel_EmptyLocked")
        : IsCategoryFilterActive ? Tr.Get("Panel_EmptyCategory")
        : Tr.Get("Panel_Empty");

    public string EmptyHint =>
        IsSearchActive ? Tr.Get("Panel_NoResultsHint")
        : IsCategoryFilterLocked ? Tr.Get("Panel_EmptyLockedHint")
        : IsCategoryFilterActive ? Tr.Get("Panel_EmptyCategoryHint")
        : Tr.Get("Panel_EmptyHint");

    // No clips means there is nothing to search; a typed query keeps the box usable so it can be edited.
    public bool CanSearch => Items.Count > 0 || IsSearchActive;

    private bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsConfirmingClear { get; set; }

    [ObservableProperty]
    public partial bool IsSelecting { get; set; }

    // Transient "Copied" feedback in the footer; the flyout hides on pick, so only History shows it.
    [ObservableProperty]
    public partial string? CopyNotice { get; set; }

    public bool HasCopyNotice => CopyNotice is not null;

    // Tests shorten this; the UI keeps it long enough to read.
    public TimeSpan CopyNoticeDuration { get; set; } = TimeSpan.FromSeconds(2.5);

    // Raised when the user picks an item; the window hides and optionally pastes.
    public event EventHandler? ItemActivated;

    // Raised after the selected clips were copied; the flyout hides.
    public event EventHandler? SelectionCopied;

    // Raised when the user asks for the full history window.
    public event EventHandler? OpenHistoryRequested;

    [RelayCommand]
    private void OpenHistory() => OpenHistoryRequested?.Invoke(this, EventArgs.Empty);

    // Raised when a row asks for its category menu; the window shows it next to the row.
    public event Action<ClipItemViewModel>? CategoryMenuRequested;

    public void RequestCategoryMenu(ClipItemViewModel item) => CategoryMenuRequested?.Invoke(item);

    // Assignment comes back as a "clip" change, so open lists refresh themselves.
    public Task AssignCategoryAsync(ClipItemViewModel item, long? categoryId) =>
        _repository.AssignCategoryAsync(item.Id, categoryId, CancellationToken.None);

    public Task<IReadOnlyList<Category>> GetCategoriesAsync() =>
        _categories.GetCategoriesAsync(CancellationToken.None);

    public Task<Category> GetOrAddCategoryAsync(string name) =>
        _categories.GetOrAddAsync(name.Trim(), CancellationToken.None);

    public Task RefreshAsync() => ReloadCommand.ExecuteAsync(null);

    public void ResetTransientState()
    {
        IsConfirmingClear = false;
        IsSelecting = false;
        SelectedItem = null;
        _noticeCts?.Cancel();
        CopyNotice = null;
    }

    // Esc: leave select mode, then cancel the clear confirmation. False = nothing left to dismiss.
    public bool DismissTransient()
    {
        if (IsSelecting)
        {
            ToggleSelectionCommand.Execute(null);
            return true;
        }

        if (IsConfirmingClear)
        {
            CancelClearCommand.Execute(null);
            return true;
        }

        return false;
    }

    private void ShowCopyNotice()
    {
        _noticeCts?.Cancel();
        var cts = _noticeCts = new CancellationTokenSource();
        CopyNotice = Tr.Get("History_Copied");
        _ = ClearCopyNoticeAsync(cts.Token);
    }

    private async Task ClearCopyNoticeAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(CopyNoticeDuration, ct);
            CopyNotice = null;
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer copy or a reset
        }
    }

    partial void OnCopyNoticeChanged(string? value)
    {
        OnPropertyChanged(nameof(HasCopyNotice));
        OnPropertyChanged(nameof(FooterText));
    }

    // Keyboard navigation: the flyout moves a selection with the arrow keys and pastes with Enter.
    [ObservableProperty]
    public partial ClipItemViewModel? SelectedItem { get; set; }

    public void MoveSelection(int delta)
    {
        if (Items.Count == 0) return;

        var index = SelectedItem is null ? -1 : Items.IndexOf(SelectedItem);
        if (index < 0)
        {
            SelectedItem = Items[delta > 0 ? 0 : Items.Count - 1];
            return;
        }

        SelectedItem = Items[Math.Clamp(index + delta, 0, Items.Count - 1)];
    }

    public void ActivateSelected()
    {
        var item = SelectedItem ?? Items.FirstOrDefault();
        if (item is not null) Activate(item);
    }

    // Quiet refresh for opening the flyout: keeps the previous list on screen while the
    // fresh query runs, and skips repainting entirely when nothing changed since last open.
    public async Task RefreshQuietAsync()
    {
        _quietRefreshRequested = true;
        if (_quietRefreshInProgress) return;

        _quietRefreshInProgress = true;
        try
        {
            while (_quietRefreshRequested)
            {
                _quietRefreshRequested = false;
                try
                {
                    var (items, total) = await LoadItemsAsync(CancellationToken.None);
                    ApplyFirstPage(items, total);
                    MarkQuietLoadCompleted(Items.Count > 0);
                }
                catch (Exception ex)
                {
                    // Keep whatever is already on screen; don't flash an error over a quick refresh.
                    MarkQuietLoadFailed(ex);
                }
            }
        }
        finally
        {
            _quietRefreshInProgress = false;
        }

        // A lock change also moves categories between locked and unlocked, which the filter shows.
        await RefreshCategoryFilterAsync();
    }

    protected override Task ReloadQuietlyAsync() => RefreshQuietAsync();

    public void Activate(ClipItemViewModel item)
    {
        if (IsSelecting)
        {
            item.IsSelected = !item.IsSelected;
            return;
        }

        // Image clips load their full image before it goes back to the clipboard.
        if (item.HasImage)
        {
            _ = ActivateImageAsync(item);
            return;
        }

        _clipboard.WriteText(item.Text);
        ShowCopyNotice();
        ItemActivated?.Invoke(this, EventArgs.Empty);
    }

    private async Task ActivateImageAsync(ClipItemViewModel item)
    {
        var image = await _repository.GetImageAsync(item.Id, CancellationToken.None);
        if (image is null) return;

        await _clipboard.WriteImageAsync(image);
        ShowCopyNotice();
        ItemActivated?.Invoke(this, EventArgs.Empty);
    }

    public Task TogglePinAsync(ClipItemViewModel item) =>
        _repository.SetPinnedAsync(item.Id, !item.IsPinned, CancellationToken.None);

    // Delete is deferred: the row disappears at once and the database delete runs when the
    // undo window closes, so a misclick never loses a clip.
    public TimeSpan DeleteUndoWindow { get; set; } = TimeSpan.FromSeconds(6);

    public bool HasPendingDelete => _pendingDeletes.Count > 0;

    public string PendingDeleteText => Tr.Get("Clip_Deleted");

    public Task DeleteAsync(ClipItemViewModel item)
    {
        var index = Items.IndexOf(item);
        if (index < 0) return Task.CompletedTask;

        var cts = new CancellationTokenSource();
        _pendingDeletes.Add(new PendingDelete(item.Id, item, index, cts));
        Items.RemoveAt(index);
        RebuildGroups();
        NotifyPendingDeleteChanged();
        _ = CommitDeleteAsync(item.Id, cts.Token);
        return Task.CompletedTask;
    }

    private async Task CommitDeleteAsync(long id, CancellationToken ct)
    {
        try
        {
            await Task.Delay(DeleteUndoWindow, ct);
        }
        catch (OperationCanceledException)
        {
            return;                                   // undone before the window closed
        }

        var pending = _pendingDeletes.FirstOrDefault(entry => entry.Id == id);
        if (pending is null) return;

        _pendingDeletes.Remove(pending);
        NotifyPendingDeleteChanged();
        await _repository.DeleteAsync(id, CancellationToken.None);
    }

    [RelayCommand]
    private void UndoDelete()
    {
        if (_pendingDeletes.Count == 0) return;

        var pending = _pendingDeletes[^1];
        _pendingDeletes.RemoveAt(_pendingDeletes.Count - 1);
        pending.Cts.Cancel();
        Items.Insert(Math.Min(pending.Index, Items.Count), pending.Item);
        RebuildGroups();
        NotifyPendingDeleteChanged();
    }

    // Clearing the history also drops pending deletes; their rows are gone, undo must not revive them.
    private void ClearPendingDeletes()
    {
        if (_pendingDeletes.Count == 0) return;

        foreach (var pending in _pendingDeletes) pending.Cts.Cancel();
        _pendingDeletes.Clear();
        NotifyPendingDeleteChanged();
    }

    private void NotifyPendingDeleteChanged()
    {
        OnPropertyChanged(nameof(HasPendingDelete));
        OnPropertyChanged(nameof(PendingDeleteText));
    }

    private sealed record PendingDelete(
        long Id, ClipItemViewModel Item, int Index, CancellationTokenSource Cts);

    protected override async Task<bool> LoadCoreAsync(CancellationToken ct)
    {
        var (items, total) = await LoadItemsAsync(ct);
        ApplyFirstPage(items, total);
        IsConfirmingClear = false;
        return Items.Count > 0;
    }

    private async Task<(IReadOnlyList<ClipListItem> Items, int Total)> LoadItemsAsync(CancellationToken ct)
    {
        var categoryId = SelectedCategoryFilter?.Value;
        var unlocked = _locks.UnlockedCategoryIds.Count == 0 ? null : _locks.UnlockedCategoryIds;
        var query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
        var items = query is null
            ? await _repository.GetRecentAsync(null, PageSize, ct, categoryId, unlocked)
            : await _repository.SearchAsync(query, null, PageSize, ct, categoryId, unlocked);
        var total = await _repository.CountAsync(query, categoryId, unlocked, ct);
        return (items, total);
    }

    // First page of a fresh query: replaces the list and resets paging.
    private void ApplyFirstPage(IReadOnlyList<ClipListItem> items, int total)
    {
        _loadGeneration++;
        _cursor = items.Count > 0 ? items[^1].Cursor : null;
        HasMore = items.Count == PageSize;
        TotalCount = total;
        ReplaceItems(items);
    }

    // Next page: appends only rows that are not already listed, then rebuilds the groups.
    private void AppendItems(IReadOnlyList<ClipListItem> page)
    {
        if (page.Count == 0) return;

        var known = Items.Select(item => item.Id)
            .Concat(_pendingDeletes.Select(entry => entry.Id))
            .ToHashSet();
        foreach (var model in page)
            if (known.Add(model.Id)) Items.Add(new ClipItemViewModel(model, _dates, this));
        RebuildGroups();
    }

    private bool CanLoadMore => HasMore && !IsLoadingMore && _cursor is not null;

    // Called by the view near the end of the scroll (IIncrementalSource).
    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    private async Task LoadMoreAsync(CancellationToken ct)
    {
        if (_cursor is null) return;

        IsLoadingMore = true;
        try
        {
            var generation = _loadGeneration;
            var categoryId = SelectedCategoryFilter?.Value;
            var unlocked = _locks.UnlockedCategoryIds.Count == 0 ? null : _locks.UnlockedCategoryIds;
            var query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            var page = query is null
                ? await _repository.GetRecentAsync(_cursor, PageSize, ct, categoryId, unlocked)
                : await _repository.SearchAsync(query, _cursor, PageSize, ct, categoryId, unlocked);

            // A refresh or a new search replaced the list while this page was loading.
            if (generation != _loadGeneration) return;

            AppendItems(page);
            if (page.Count > 0) _cursor = page[^1].Cursor;
            HasMore = page.Count == PageSize;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    partial void OnHasMoreChanged(bool value) => LoadMoreCommand.NotifyCanExecuteChanged();

    partial void OnIsLoadingMoreChanged(bool value) => LoadMoreCommand.NotifyCanExecuteChanged();

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(FooterText));
    }

    private void ReplaceItems(IEnumerable<ClipListItem> items)
    {
        var existing = Items.ToDictionary(item => item.Id);
        var desired = new List<ClipItemViewModel>();
        foreach (var model in items)
        {
            if (_pendingDeletes.Any(entry => entry.Id == model.Id)) continue;   // still waiting on undo

            if (existing.TryGetValue(model.Id, out var item))
            {
                item.UpdateFrom(model, _dates);
                desired.Add(item);
            }
            else
            {
                desired.Add(new ClipItemViewModel(model, _dates, this));
            }
        }

        ReconcileItems(Items, desired);
        RebuildGroups();
    }

    private void RebuildGroups()
    {
        var desired = new List<(string TitleKey, ClipItemViewModel[] Items)>();
        AddGroup(desired, "History_Pinned", Items.Where(i => i.IsPinned));
        AddGroup(desired, "History_Recent", Items.Where(i => !i.IsPinned));

        var groups = new List<ClipGroup>();
        foreach (var (titleKey, items) in desired)
        {
            var title = Tr.Get(titleKey);
            var group = Groups.FirstOrDefault(candidate => candidate.Title == title) ?? new ClipGroup(title);
            ReconcileItems(group, items);
            groups.Add(group);
        }

        for (var index = Groups.Count - 1; index >= 0; index--)
            if (!groups.Contains(Groups[index])) Groups.RemoveAt(index);

        for (var index = 0; index < groups.Count; index++)
        {
            if (index < Groups.Count && ReferenceEquals(Groups[index], groups[index])) continue;

            var currentIndex = Groups.IndexOf(groups[index]);
            if (currentIndex >= 0)
                Groups.Move(currentIndex, index);
            else
                Groups.Insert(index, groups[index]);
        }

        while (Groups.Count > groups.Count) Groups.RemoveAt(Groups.Count - 1);
    }

    private static void ReconcileItems(
        ObservableCollection<ClipItemViewModel> collection,
        IReadOnlyList<ClipItemViewModel> desired)
    {
        var desiredIds = desired.Select(item => item.Id).ToHashSet();
        for (var index = collection.Count - 1; index >= 0; index--)
            if (!desiredIds.Contains(collection[index].Id)) collection.RemoveAt(index);

        for (var index = 0; index < desired.Count; index++)
        {
            if (index < collection.Count && ReferenceEquals(collection[index], desired[index])) continue;

            var currentIndex = collection.IndexOf(desired[index]);
            if (currentIndex >= 0)
                collection.Move(currentIndex, index);
            else
                collection.Insert(index, desired[index]);
        }

        while (collection.Count > desired.Count) collection.RemoveAt(collection.Count - 1);
    }

    private static void AddGroup(
        ICollection<(string TitleKey, ClipItemViewModel[] Items)> groups,
        string titleKey,
        IEnumerable<ClipItemViewModel> items)
    {
        var rows = items.ToArray();
        if (rows.Length > 0) groups.Add((titleKey, rows));
    }

    // Destructive: the Clear button shows an in-window confirmation panel first.
    [RelayCommand]
    private void RequestClear() => IsConfirmingClear = true;

    [RelayCommand]
    private void CancelClear() => IsConfirmingClear = false;

    // One command for both the Select and Cancel buttons.
    [RelayCommand]
    private void ToggleSelection() => IsSelecting = !IsSelecting;

    [RelayCommand]
    private void CopySelected()
    {
        // Images have no text to join; they are left out of a multi-copy.
        var texts = Items.Where(item => item.IsSelected && !item.HasImage).Select(item => item.Text).ToArray();
        if (texts.Length == 0) return;

        _clipboard.WriteText(string.Join("\r\n", texts));
        ShowCopyNotice();
        IsSelecting = false;
        SelectionCopied?.Invoke(this, EventArgs.Empty);
    }

    // Called by a row when its check changes so the footer count keeps up.
    internal void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectionCountText));
        OnPropertyChanged(nameof(CanCopySelection));
        OnPropertyChanged(nameof(FooterText));
    }

    [RelayCommand]
    private async Task ConfirmClearAsync()
    {
        await _repository.ClearUnpinnedAsync(CancellationToken.None);
        ClearPendingDeletes();                        // their rows are gone; undo must not resurrect them
        IsConfirmingClear = false;
    }

    partial void OnIsSelectingChanged(bool value)
    {
        if (!value)
            foreach (var item in Items) item.IsSelected = false;

        foreach (var item in Items) item.NotifySelectionModeChanged();
        OnPropertyChanged(nameof(ShowSelectButton));
        OnPropertyChanged(nameof(FooterText));
    }

    // Debounced: the previous query is cancelled as soon as a newer one arrives.
    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyHint));
        OnPropertyChanged(nameof(CanSearch));
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        _ = SearchDebouncedAsync(cts.Token);
    }

    private async Task SearchDebouncedAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(SearchDebounceMs, ct);
            if (!ct.IsCancellationRequested) await RefreshAsync();
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer query
        }
    }
}
