namespace ClipShelf.App.Services;

// Async so the same interface works for WPF message boxes and WinUI ContentDialogs.
public interface IDialogService
{
    Task ShowInfoAsync(string message);

    Task ShowErrorAsync(string message);

    Task<bool> ConfirmAsync(string message);
}
