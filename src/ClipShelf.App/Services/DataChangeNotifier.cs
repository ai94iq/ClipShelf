using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;

namespace ClipShelf.App.Services;

// Repositories call this from background threads, so it hops to the UI thread before messaging.
// Created on the UI thread at startup (App.OnLaunched), which captures the right DispatcherQueue.
public sealed class DataChangeNotifier : IDataChangeNotifier
{
    private readonly DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

    public void Notify(DataChanged change) =>
        _queue.TryEnqueue(() => WeakReferenceMessenger.Default.Send(change));
}
