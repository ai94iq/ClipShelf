namespace ClipShelf.App.Services;

// Async so the same interface works for WPF message boxes and WinUI ContentDialogs.
public interface IDialogService
{
    Task ShowInfoAsync(string message);

    Task ShowErrorAsync(string message);

    // Confirms a destructive action. Both button labels are verbs, and Cancel is the default.
    Task<bool> ConfirmAsync(string message, string confirmText, string cancelText);
}
