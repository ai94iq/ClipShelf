using Avalonia.Threading;

namespace ClipShelf.Desktop.Platform.Windows;

// Marshals work onto the UI thread through Avalonia's dispatcher.
internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
