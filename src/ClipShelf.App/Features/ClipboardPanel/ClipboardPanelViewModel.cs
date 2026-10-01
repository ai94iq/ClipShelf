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

    public ClipboardPanelViewModel(
        IClipRepository repository,
        IClipboardWriter clipboard,
        IDateFormatter dates,
        ILogger<ClipboardPanelViewModel> log) : base(log)
    {
        _repository = repository;
        _clipboard = clipboard;
        _dates = dates;
        ReloadOnChange("clip");
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
        var items = string.IsNullOrWhiteSpace(SearchText)
            ? await _repository.GetRecentAsync(null, PageSize, ct)
            : await _repository.SearchAsync(SearchText, null, PageSize, ct);

        Items.Clear();
        foreach (var item in items) Items.Add(new ClipItemViewModel(item, _dates, this));
        IsConfirmingClear = false;
        RebuildGroups();
        return Items.Count > 0;
    }

    private void RebuildGroups()
    {
        Groups.Clear();
        AddGroup("History_Pinned", Items.Where(i => i.IsPinned));
        AddGroup("History_Recent", Items.Where(i => !i.IsPinned));
    }

    private void AddGroup(string titleKey, IEnumerable<ClipItemViewModel> items)
    {
        var group = new ClipGroup(Tr.Get(titleKey));
        foreach (var item in items) group.Add(item);
        if (group.Count > 0) Groups.Add(group);
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
