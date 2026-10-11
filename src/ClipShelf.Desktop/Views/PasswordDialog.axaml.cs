using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipShelf.Desktop.Views;

// The lock-password dialog in its four modes, mirroring the WinUI flows: verify the current
// password, set a new one, change it (current plus new), or confirm removing it. The result box
// carries a bool for the verify and remove modes and the new password (string) for the set and
// change modes; null means the dialog was cancelled or closed with its owner.
public sealed partial class PasswordDialog : Window
{
    private enum Mode
    {
        Verify,
        Set,
        Change,
        Remove,
    }

    private readonly TaskCompletionSource<object?> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Mode _mode;
    private readonly Func<string, bool>? _verify;

    // Public for the XAML loader; runtime call sites use the PasswordDialogs entry points below.
    public PasswordDialog()
        : this(Mode.Verify, null)
    {
    }

    private PasswordDialog(Mode mode, Func<string, bool>? verify)
    {
        _mode = mode;
        _verify = verify;
        InitializeComponent();

        Title = mode switch
        {
            Mode.Verify => Tr.Get("Password_VerifyTitle"),
            Mode.Set => Tr.Get("Password_SetTitle"),
            Mode.Change => Tr.Get("Password_ChangeTitle"),
            _ => Tr.Get("Password_RemoveTitle"),
        };

        PrimaryButton.Content = mode switch
        {
            Mode.Verify => Tr.Get("Common_Ok"),
            Mode.Remove => Tr.Get("Common_Remove"),
            _ => Tr.Get("Common_Save"),
        };
        CancelButton.Content = Tr.Get("Common_Cancel");

        CurrentPasswordBox.Watermark = Tr.Get("Password_Current");
        NewPasswordBox.Watermark = Tr.Get("Password_New");
        ConfirmPasswordBox.Watermark = Tr.Get("Password_Confirm");
        CurrentPasswordBox.IsVisible = mode is Mode.Verify or Mode.Change or Mode.Remove;
        NewPasswordBox.IsVisible = mode is Mode.Set or Mode.Change;
        ConfirmPasswordBox.IsVisible = mode is Mode.Set or Mode.Change;

        if (mode == Mode.Remove)
        {
            WarningText.Text = Tr.Get("Password_RemoveWarning");
            WarningText.IsVisible = true;
        }

        CurrentPasswordBox.TextChanged += (_, _) => RefreshPrimary();
        NewPasswordBox.TextChanged += (_, _) => RefreshPrimary();
        ConfirmPasswordBox.TextChanged += (_, _) => RefreshPrimary();
        RefreshPrimary();
    }

    // The primary stays disabled until every visible box has input.
    private void RefreshPrimary()
    {
        PrimaryButton.IsEnabled = _mode switch
        {
            Mode.Verify => !string.IsNullOrEmpty(CurrentPasswordBox.Text),
            Mode.Set => !string.IsNullOrEmpty(NewPasswordBox.Text) && !string.IsNullOrEmpty(ConfirmPasswordBox.Text),
            Mode.Change => !string.IsNullOrEmpty(CurrentPasswordBox.Text)
                && !string.IsNullOrEmpty(NewPasswordBox.Text)
                && !string.IsNullOrEmpty(ConfirmPasswordBox.Text),
            _ => !string.IsNullOrEmpty(CurrentPasswordBox.Text),
        };
    }

    private static bool TryCreate(string password, string confirm, out string error)
    {
        if (password.Length < 4)
        {
            error = Tr.Get("Password_TooShort");
            return false;
        }

        if (password != confirm)
        {
            error = Tr.Get("Password_Mismatch");
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _result.TrySetResult(null);
        base.OnClosed(e);
    }

    private void OnPrimaryClick(object? sender, RoutedEventArgs e)
    {
        switch (_mode)
        {
            case Mode.Verify:
                if (_verify != null && _verify(CurrentPasswordBox.Text ?? string.Empty))
                {
                    _result.TrySetResult(true);
                    Close();
                }
                else
                {
                    ShowError(Tr.Get("Password_Wrong"));
                }

                break;

            case Mode.Set:
                if (TryCreate(NewPasswordBox.Text ?? string.Empty, ConfirmPasswordBox.Text ?? string.Empty, out var setError))
                {
                    _result.TrySetResult(NewPasswordBox.Text);
                    Close();
                }
                else
                {
                    ShowError(setError);
                }

                break;

            case Mode.Change:
                if (_verify == null || !_verify(CurrentPasswordBox.Text ?? string.Empty))
                {
                    ShowError(Tr.Get("Password_Wrong"));
                }
                else if (TryCreate(
                    NewPasswordBox.Text ?? string.Empty, ConfirmPasswordBox.Text ?? string.Empty, out var changeError))
                {
                    _result.TrySetResult(NewPasswordBox.Text);
                    Close();
                }
                else
                {
                    ShowError(changeError);
                }

                break;

            default:
                if (_verify != null && _verify(CurrentPasswordBox.Text ?? string.Empty))
                {
                    _result.TrySetResult(true);
                    Close();
                }
                else
                {
                    ShowError(Tr.Get("Password_Wrong"));
                }

                break;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    internal static async Task<bool> VerifyAsync(Window owner, Func<string, bool> verify)
    {
        var result = await RunAsync(owner, Mode.Verify, verify);
        return (bool)(result ?? false);
    }

    internal static async Task<string?> SetAsync(Window owner) =>
        await RunAsync(owner, Mode.Set, null) as string;

    internal static async Task<string?> ChangeAsync(Window owner, Func<string, bool> verify) =>
        await RunAsync(owner, Mode.Change, verify) as string;

    internal static async Task<bool> ConfirmRemoveAsync(Window owner, Func<string, bool> verify)
    {
        var result = await RunAsync(owner, Mode.Remove, verify);
        return (bool)(result ?? false);
    }

    private static async Task<object?> RunAsync(Window owner, Mode mode, Func<string, bool>? verify)
    {
        var dialog = new PasswordDialog(mode, verify);
        await dialog.ShowDialog(owner);
        return await dialog._result.Task;
    }
}

// The call-site API for the password flows. Verify and ConfirmRemove return whether the password
// checked out; Set and Change return the new password, or null when cancelled.
public static class PasswordDialogs
{
    public static Task<bool> VerifyAsync(Window owner, Func<string, bool> verify) =>
        PasswordDialog.VerifyAsync(owner, verify);

    public static Task<string?> SetAsync(Window owner) =>
        PasswordDialog.SetAsync(owner);

    public static Task<string?> ChangeAsync(Window owner, Func<string, bool> verify) =>
        PasswordDialog.ChangeAsync(owner, verify);

    internal static Task<bool> ConfirmRemoveAsync(Window owner, Func<string, bool> verify) =>
        PasswordDialog.ConfirmRemoveAsync(owner, verify);
}