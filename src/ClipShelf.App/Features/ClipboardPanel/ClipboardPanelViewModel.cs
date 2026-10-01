using System.Collections.ObjectModel;
using ClipShelf.Core.Abstractions;

namespace ClipShelf.App.Features.ClipboardPanel;

// Drives the floating history list: load, search, copy, pin, delete and clear.
public sealed partial class ClipboardPanelViewModel : PageViewModel
{
    private const int PageSize = 50;
    private const int SearchDebounceMs = 300;

    private readonly IClipRepository _repository;
    private readonly IClipboardWriter _clipboard;
    private readonly IDateFormatter _dates;
    private CancellationTokenSource? _searchCts;
    private bool _quietRefreshInProgress;
    private bool _quietRefreshRequested;

    public ClipboardPanelViewModel(
        IClipRepository repository,
        IClipboardWriter clipboard,
        IDateFormatter dates,
        ILogger<ClipboardPanelViewModel> log) : base(log)
    {
        _repository = repository;
        _clipboard = clipboard;
        _dates = dates;
        ReloadQuietlyOnChange("clip");
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(CountText));
        };
    }

    public ObservableCollection<ClipItemViewModel> Items { get; } = [];

    // Grouped view (Pinned, then Recent) used by the full history page.
    public ObservableCollection<ClipGroup> Groups { get; } = [];

    public bool HasItems => Items.Count > 0;

    public string CountText => Tr.Format("History_Count", Items.Count);

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsConfirmingClear { get; set; }

    // Raised when the user picks an item; the window hides and optionally pastes.
    public event EventHandler? ItemActivated;

    // Raised when the user asks for the full history window.
    public event EventHandler? OpenHistoryRequested;

    [RelayCommand]
    private void OpenHistory() => OpenHistoryRequested?.Invoke(this, EventArgs.Empty);

    public Task RefreshAsync() => ReloadCommand.ExecuteAsync(null);

    public void ResetTransientState() => IsConfirmingClear = false;

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
                    ReplaceItems(await LoadItemsAsync(CancellationToken.None));
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
    }

    protected override Task ReloadQuietlyAsync() => RefreshQuietAsync();

    public void Activate(ClipItemViewModel item)
    {
        _clipboard.WriteText(item.Text);
        ItemActivated?.Invoke(this, EventArgs.Empty);
    }

    public Task TogglePinAsync(ClipItemViewModel item) =>
        _repository.SetPinnedAsync(item.Id, !item.IsPinned, CancellationToken.None);

    public Task DeleteAsync(ClipItemViewModel item) =>
        _repository.DeleteAsync(item.Id, CancellationToken.None);

    protected override async Task<bool> LoadCoreAsync(CancellationToken ct)
    {
        ReplaceItems(await LoadItemsAsync(ct));
        IsConfirmingClear = false;
        return Items.Count > 0;
    }

    private async Task<IReadOnlyList<ClipListItem>> LoadItemsAsync(CancellationToken ct)
    {
        return string.IsNullOrWhiteSpace(SearchText)
            ? await _repository.GetRecentAsync(null, PageSize, ct)
            : await _repository.SearchAsync(SearchText, null, PageSize, ct);
    }

    private void ReplaceItems(IEnumerable<ClipListItem> items)
    {
        var existing = Items.ToDictionary(item => item.Id);
        var desired = new List<ClipItemViewModel>();
        foreach (var model in items)
        {
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

    [RelayCommand]
    private async Task ConfirmClearAsync()
    {
        await _repository.ClearUnpinnedAsync(CancellationToken.None);
        IsConfirmingClear = false;
    }

    // Debounced: the previous query is cancelled as soon as a newer one arrives.
    partial void OnSearchTextChanged(string value)
    {
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
