using Microsoft.UI.Dispatching;

namespace ClipShelf.App.Platform;

// Captures the UI thread's dispatcher queue; created on the UI thread at startup.
internal sealed class DispatcherQueueUiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

    public void Post(Action action) => _queue.TryEnqueue(() => action());
}
