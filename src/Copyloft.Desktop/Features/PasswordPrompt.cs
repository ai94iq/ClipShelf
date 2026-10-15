using Avalonia;
using Avalonia.Controls;
using Copyloft.Desktop.Controls;

namespace Copyloft.Desktop.Features;

// The shared lock password: set, change, remove, and verify before exporting or unlocking.
internal static class PasswordPrompt
{
    private const int MinimumLength = 4;

    // Asks again until the password verifies, or the user cancels.
    public static Task<bool> VerifyAsync(Visual anchor, Func<string, bool> verify)
    {
        var box = PasswordBox("Password_Current");
        var dialog = new InlineDialog(Tr.Get("Password_VerifyTitle"), Tr.Get("Common_Ok"), box);
        dialog.Accept = () => Task.FromResult(Check(dialog, verify(Text(box)), "Password_Wrong"));
        return dialog.ShowAsync(anchor);
    }

    // Setting a password for the first time; returns it, or null when cancelled.
    public static async Task<string?> SetAsync(Visual anchor)
    {
        var password = PasswordBox("Password_New");
        var confirm = PasswordBox("Password_Confirm");
        var dialog = new InlineDialog(Tr.Get("Password_SetTitle"), Tr.Get("Common_Save"), password, confirm);
        dialog.Accept = () => Task.FromResult(TryCreate(dialog, Text(password), Text(confirm)));
        return await dialog.ShowAsync(anchor) ? Text(password) : null;
    }

    // Changing: the current password must verify first.
    public static async Task<string?> ChangeAsync(Visual anchor, Func<string, bool> verify)
    {
        var current = PasswordBox("Password_Current");
        var password = PasswordBox("Password_New");
        var confirm = PasswordBox("Password_Confirm");
        var dialog = new InlineDialog(Tr.Get("Password_ChangeTitle"), Tr.Get("Common_Save"), current, password, confirm);
        dialog.Accept = () => Task.FromResult(
            Check(dialog, verify(Text(current)), "Password_Wrong") && TryCreate(dialog, Text(password), Text(confirm)));
        return await dialog.ShowAsync(anchor) ? Text(password) : null;
    }

    // Removing: the current password is still required; the warning spells out the consequence.
    public static Task<bool> ConfirmRemoveAsync(Visual anchor, Func<string, bool> verify)
    {
        var warning = new TextBlock { Text = Tr.Get("Password_RemoveWarning"), TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var current = PasswordBox("Password_Current");
        var dialog = new InlineDialog(Tr.Get("Password_RemoveTitle"), Tr.Get("Common_Remove"), warning, current);
        dialog.Accept = () => Task.FromResult(Check(dialog, verify(Text(current)), "Password_Wrong"));
        return dialog.ShowAsync(anchor);
    }

    private static bool TryCreate(InlineDialog dialog, string password, string confirm) =>
        Check(dialog, password.Length >= MinimumLength, "Password_TooShort") &&
        Check(dialog, password == confirm, "Password_Mismatch");

    private static bool Check(InlineDialog dialog, bool ok, string errorKey)
    {
        if (!ok) dialog.ShowError(Tr.Get(errorKey));
        return ok;
    }

    private static string Text(TextBox box) => box.Text ?? string.Empty;

    private static TextBox PasswordBox(string placeholderKey) => new()
    {
        PasswordChar = '●',
        Classes = { "revealPasswordButton" },
        PlaceholderText = Tr.Get(placeholderKey),
        [Avalonia.Automation.AutomationProperties.NameProperty] = Tr.Get(placeholderKey),
    };
}
