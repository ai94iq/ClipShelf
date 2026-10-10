using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipShelf.Desktop.Views;

// A generic confirmation: a message plus a destructive primary action and cancel. Returns true only
// when the primary button is pressed; cancel and closing with the owner return false.
public sealed partial class ConfirmDialog : Window
{
    private readonly TaskCompletionSource<bool> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Public for the XAML loader; runtime call sites use ShowAsync.
    public ConfirmDialog()
        : this(string.Empty, string.Empty, string.Empty)
    {
    }

    private ConfirmDialog(string title, string message, string primaryLabel)
    {
        InitializeComponent();

        Title = title;
        MessageText.Text = message;
        PrimaryButton.Content = primaryLabel;
        CancelButton.Content = Tr.Get("Common_Cancel");
    }

    public static async Task<bool> ShowAsync(
        Window owner, string title, string message, string primaryLabel)
    {
        var dialog = new ConfirmDialog(title, message, primaryLabel);
        await dialog.ShowDialog(owner);
        return await dialog._result.Task;
    }

    protected override void OnClosed(EventArgs e)
    {
        _result.TrySetResult(false);
        base.OnClosed(e);
    }

    private void OnPrimaryClick(object? sender, RoutedEventArgs e)
    {
        _result.TrySetResult(true);
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
