using Microsoft.UI.Xaml.Controls;
using ClipShelf.App.Shell;

namespace ClipShelf.App.Services;

// Every info, error and confirm dialog goes through here so RTL is handled once.
// Lazy<MainWindow> avoids a circular dependency (window → view model → dialogs → window).
public sealed class DialogService(Lazy<MainWindow> window) : IDialogService
{
    public async Task ShowInfoAsync(string message) => await ShowAsync(message, Tr.Get("Common_Ok"), null);

    public async Task ShowErrorAsync(string message) => await ShowAsync(message, Tr.Get("Common_Ok"), null);

    public async Task<bool> ConfirmAsync(string message) =>
        await ShowAsync(message, Tr.Get("Common_Yes"), Tr.Get("Common_No")) == ContentDialogResult.Primary;

    private async Task<ContentDialogResult> ShowAsync(string message, string primary, string? close)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = window.Value.Content.XamlRoot,
            FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Title = Tr.Get("App_Name"),
            Content = message,
            PrimaryButtonText = primary,
            CloseButtonText = close ?? string.Empty,
            DefaultButton = close is null ? ContentDialogButton.Primary : ContentDialogButton.Close,
        };
        return await dialog.ShowAsync();
    }
}
