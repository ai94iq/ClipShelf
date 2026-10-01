using Microsoft.UI.Xaml.Controls;

namespace ClipShelf.App.Services;

// Every info, error and confirm dialog goes through here. The dialog attaches to the active
// window's XamlRoot, so the same service works for the shell and the tray flyout.
public sealed class DialogService(WindowContext context) : IDialogService
{
    public async Task ShowInfoAsync(string message) => await ShowAsync(message, Tr.Get("Common_Ok"), null);

    public async Task ShowErrorAsync(string message) => await ShowAsync(message, Tr.Get("Common_Ok"), null);

    public async Task<bool> ConfirmAsync(string message, string confirmText, string cancelText) =>
        await ShowAsync(message, confirmText, cancelText) == ContentDialogResult.Primary;

    private async Task<ContentDialogResult> ShowAsync(string message, string primary, string? close)
    {
        var root = context.Active?.Content?.XamlRoot;
        if (root is null) return ContentDialogResult.None;

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Content = message,
            PrimaryButtonText = primary,
            CloseButtonText = close ?? string.Empty,
            DefaultButton = close is null ? ContentDialogButton.Primary : ContentDialogButton.Close,
        };
        return await dialog.ShowAsync();
    }
}
