using Avalonia.Threading;
using ClipShelf.Presentation.Platform;

namespace ClipShelf.Desktop.Platform;

// Marshals view-model work onto Avalonia's UI thread.
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
