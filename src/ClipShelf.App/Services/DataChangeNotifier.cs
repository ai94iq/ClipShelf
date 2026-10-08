using CommunityToolkit.Mvvm.Messaging;

namespace ClipShelf.App.Services;

// Repositories call this from background threads, so it hops to the UI thread before messaging.
// The dispatcher is captured by the IUiDispatcher implementation on the UI thread at startup.
public sealed class DataChangeNotifier : IDataChangeNotifier
{
    private readonly IUiDispatcher _dispatcher;

    public DataChangeNotifier(IUiDispatcher dispatcher) => _dispatcher = dispatcher;

    public void Notify(DataChanged change) =>
        _dispatcher.Post(() => WeakReferenceMessenger.Default.Send(change));
}
