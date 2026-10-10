using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipShelf.Desktop.Views;

// Asks for a new category name; returns null when cancelled or closed.
public sealed partial class NewCategoryDialog : Window
{
    private readonly TaskCompletionSource<string?> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public NewCategoryDialog() => InitializeComponent();

    public static async Task<string?> ShowAsync(Window owner)
    {
        var dialog = new NewCategoryDialog();
        await dialog.ShowDialog(owner);
        return await dialog._result.Task;
    }

    protected override void OnClosed(EventArgs e)
    {
        _result.TrySetResult(null);
        base.OnClosed(e);
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        _result.TrySetResult(name);
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
