namespace ClipShelf.Presentation.Platform;

// Marshals work onto the UI thread; implementations capture their own thread's dispatcher.
public interface IUiDispatcher
{
    void Post(Action action);
}
