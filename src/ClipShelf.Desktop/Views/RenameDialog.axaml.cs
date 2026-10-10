using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipShelf.Desktop.Views;

// Asks for a replacement category name. The checkTaken callback returns true when the name is
// already used; on a taken name the dialog stays open and shows the taken-name error. Returns the
// accepted name, or null when cancelled or closed with its owner.
public sealed partial class RenameDialog : Window
{
    private readonly TaskCompletionSource<string?> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Func<string, Task<bool>> _checkTaken;

    // Public for the XAML loader; runtime call sites use ShowAsync.
    public RenameDialog()
        : this(string.Empty, _ => Task.FromResult(false))
    {
    }

    private RenameDialog(string currentName, Func<string, Task<bool>> checkTaken)
    {
        _checkTaken = checkTaken;
        InitializeComponent();

        Title = Tr.Get("Category_RenameTitle");
        PrimaryButton.Content = Tr.Get("Common_Save");
        CancelButton.Content = Tr.Get("Common_Cancel");
        NameBox.Text = currentName;
        RefreshPrimary();

        NameBox.TextChanged += (_, _) => RefreshPrimary();
    }

    public static async Task<string?> ShowAsync(
        Window owner, string currentName, Func<string, Task<bool>> checkTaken)
    {
        var dialog = new RenameDialog(currentName, checkTaken);
        await dialog.ShowDialog(owner);
        return await dialog._result.Task;
    }

    protected override void OnClosed(EventArgs e)
    {
        _result.TrySetResult(null);
        base.OnClosed(e);
    }

    private void RefreshPrimary() =>
        PrimaryButton.IsEnabled = !string.IsNullOrWhiteSpace(NameBox.Text);

    private async void OnPrimaryClick(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name)) return;

        if (await _checkTaken(name))
        {
            ErrorText.Text = Tr.Get("Category_Exists");
            ErrorText.IsVisible = true;
            return;
        }

        _result.TrySetResult(name);
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
