using CommunityToolkit.Mvvm.Messaging;

namespace ClipShelf.App.Common;

// Base for every page ViewModel: one implementation of loading, empty, error and retry.
// Derived pages implement LoadCoreAsync and return false when there is nothing to show.
public abstract partial class PageViewModel : ObservableObject
{
    private readonly ILogger _log;
    private bool _loadedOnce;

    protected PageViewModel(ILogger log)
    {
        _log = log;
        State = LoadState.Idle;
    }

    [ObservableProperty]
    public partial LoadState State { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // Call from the page's navigated-to hook. Loads the first time only; later visits reuse the data.
    public Task EnsureLoadedAsync() =>
        !_loadedOnce && ReloadCommand.CanExecute(null) ? ReloadCommand.ExecuteAsync(null) : Task.CompletedTask;

    protected abstract Task<bool> LoadCoreAsync(CancellationToken ct);

    protected void MarkQuietLoadCompleted(bool hasContent)
    {
        _loadedOnce = true;
        ErrorMessage = null;
        State = hasContent ? LoadState.Loaded : LoadState.Empty;
    }

    protected void MarkQuietLoadFailed(Exception ex)
    {
        _log.LogError(ex, "Loading {Page} failed", GetType().Name);
        if (_loadedOnce) return;

        ErrorMessage = Tr.Get("Error_LoadFailed");
        State = LoadState.Error;
    }

    // Reload automatically when a repository reports a change in one of these areas.
    protected void ReloadOnChange(params string[] areas) =>
        WeakReferenceMessenger.Default.Register<PageViewModel, DataChanged>(this, (vm, message) =>
        {
            if (areas.Contains(message.Area) && vm._loadedOnce && vm.ReloadCommand.CanExecute(null))
                vm.ReloadCommand.Execute(null);
        });

    protected void ReloadQuietlyOnChange(params string[] areas) =>
        WeakReferenceMessenger.Default.Register<PageViewModel, DataChanged>(this, (vm, message) =>
        {
            if (areas.Contains(message.Area) && vm._loadedOnce)
                _ = vm.ReloadQuietlyAsync();
        });

    protected virtual Task ReloadQuietlyAsync() => ReloadCommand.ExecuteAsync(null);

    [RelayCommand]
    private async Task ReloadAsync(CancellationToken ct)
    {
        State = LoadState.Loading;
        ErrorMessage = null;
        try
        {
            State = await LoadCoreAsync(ct) ? LoadState.Loaded : LoadState.Empty;
            _loadedOnce = true;
        }
        catch (OperationCanceledException)
        {
            State = LoadState.Idle;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Loading {Page} failed", GetType().Name);
            ErrorMessage = Tr.Get("Error_LoadFailed");
            State = LoadState.Error;
        }
    }
}
