namespace ClipShelf.App.Services;

// Implemented by ViewModels of paged lists; the view calls LoadMoreCommand near the end of the scroll.
public interface IIncrementalSource
{
    bool HasMore { get; }

    IAsyncRelayCommand LoadMoreCommand { get; }
}
