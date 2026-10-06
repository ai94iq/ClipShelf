using ClipShelf.App.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ClipShelf.App.Features.Settings;

// Password prompts for the export lock: set, change, remove, and verify before exporting.
internal static class PasswordPrompt
{
    // The export gate: asks again until the password verifies, or the user cancels.
    public static async Task<bool> VerifyAsync(XamlRoot root, Func<string, bool> verify)
    {
        var box = new PasswordBox { PlaceholderText = Tr.Get("Password_Current") };
        var error = ErrorText();
        var dialog = Build(root, Tr.Get("Password_VerifyTitle"), Tr.Get("Common_Ok"), [box, error]);
        var verified = false;
        dialog.PrimaryButtonClick += (_, args) =>
        {
            verified = verify(box.Password);
            if (verified) return;
            error.Text = Tr.Get("Password_Wrong");
            error.Visibility = Visibility.Visible;
            args.Cancel = true;
        };
        await dialog.ShowAsync();
        return verified;
    }

    // Setting a password for the first time; returns it, or null when cancelled.
    public static async Task<string?> SetAsync(XamlRoot root)
    {
        var password = new PasswordBox { PlaceholderText = Tr.Get("Password_New") };
        var confirm = new PasswordBox { PlaceholderText = Tr.Get("Password_Confirm") };
        var error = ErrorText();
        var dialog = Build(root, Tr.Get("Password_SetTitle"), Tr.Get("Common_Save"), [password, confirm, error]);
        string? created = null;
        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (!TryCreate(password.Password, confirm.Password, error))
            {
                args.Cancel = true;
                return;
            }

            created = password.Password;
        };
        await dialog.ShowAsync();
        return created;
    }

    // Changing: the current password must verify first.
    public static async Task<string?> ChangeAsync(XamlRoot root, Func<string, bool> verify)
    {
        var current = new PasswordBox { PlaceholderText = Tr.Get("Password_Current") };
        var password = new PasswordBox { PlaceholderText = Tr.Get("Password_New") };
        var confirm = new PasswordBox { PlaceholderText = Tr.Get("Password_Confirm") };
        var error = ErrorText();
        var dialog = Build(
            root, Tr.Get("Password_ChangeTitle"), Tr.Get("Common_Save"), [current, password, confirm, error]);
        string? created = null;
        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (!verify(current.Password) || !TryCreate(password.Password, confirm.Password, error))
            {
                if (error.Visibility != Visibility.Visible) Show(error, Tr.Get("Password_Wrong"));
                args.Cancel = true;
                return;
            }

            created = password.Password;
        };
        await dialog.ShowAsync();
        return created;
    }

    // Removing: the current password is still required; the warning spells out the consequence.
    public static async Task<bool> ConfirmRemoveAsync(XamlRoot root, Func<string, bool> verify)
    {
        var warning = new TextBlock
        {
            Text = Tr.Get("Password_RemoveWarning"),
            TextWrapping = TextWrapping.Wrap,
        };
        var current = new PasswordBox { PlaceholderText = Tr.Get("Password_Current") };
        var error = ErrorText();
        var dialog = Build(root, Tr.Get("Password_RemoveTitle"), Tr.Get("Common_Remove"), [warning, current, error]);
        var removed = false;
        dialog.PrimaryButtonClick += (_, args) =>
        {
            removed = verify(current.Password);
            if (removed) return;
            Show(error, Tr.Get("Password_Wrong"));
            args.Cancel = true;
        };
        await dialog.ShowAsync();
        return removed;
    }

    private static bool TryCreate(string password, string confirm, TextBlock error)
    {
        if (password.Length < 4)
        {
            Show(error, Tr.Get("Password_TooShort"));
            return false;
        }

        if (password != confirm)
        {
            Show(error, Tr.Get("Password_Mismatch"));
            return false;
        }

        return true;
    }

    private static void Show(TextBlock error, string message)
    {
        error.Text = message;
        error.Visibility = Visibility.Visible;
    }

    private static ContentDialog Build(XamlRoot root, string title, string primary, UIElement[] content)
    {
        var panel = new StackPanel { Spacing = 8 };
        foreach (var element in content) panel.Children.Add(element);

        return new ContentDialog
        {
            XamlRoot = root,
            Title = title,
            Content = panel,
            PrimaryButtonText = primary,
            CloseButtonText = Tr.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
    }

    private static TextBlock ErrorText() => new()
    {
        Style = (Style)Application.Current.Resources["ErrorTextStyle"],
        Visibility = Visibility.Collapsed,
    };
}
