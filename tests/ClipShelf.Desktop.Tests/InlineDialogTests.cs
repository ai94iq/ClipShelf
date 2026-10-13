using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClipShelf.Desktop.Controls;

namespace ClipShelf.Desktop.Tests;

// Drives an open InlineDialog the way a user would.
internal static class DialogDriver
{
    public static Window Window()
    {
        var window = new Window { Width = 400, Height = 400, Content = new Panel() };
        window.Show();
        return window;
    }

    public static InlineDialog Open(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        return InlineDialog.OpenIn(window) ?? throw new InvalidOperationException("no dialog is open");
    }

    public static IReadOnlyList<TextBox> Boxes(InlineDialog dialog) =>
        dialog.GetLogicalDescendantsOf<TextBox>();

    public static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    // Lays the window out first: until then a dialog's content has no visual route to it.
    public static void Press(Control target, Key key)
    {
        Dispatcher.UIThread.RunJobs();
        (Avalonia.Controls.TopLevel.GetTopLevel(target) as Window)?.UpdateLayout();
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        Dispatcher.UIThread.RunJobs();
    }

    private static IReadOnlyList<T> GetLogicalDescendantsOf<T>(this InlineDialog dialog) =>
        Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(dialog).OfType<T>().ToList();
}

public sealed class InlineDialogTests
{
    private static (InlineDialog Dialog, TextBox Box) Build()
    {
        var box = new TextBox();
        return (new InlineDialog("Rename", "Save", box), box);
    }

    [Fact]
    public Task A_dialog_shows_over_the_window_and_focuses_its_input() => Ui.Run(() =>
    {
        var window = DialogDriver.Window();
        var (dialog, box) = Build();

        _ = dialog.ShowAsync(window);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(dialog, InlineDialog.OpenIn(window));
        Assert.Equal("Rename", dialog.FindControl<TextBlock>("TitleText")!.Text);
        Assert.True(box.IsFocused);
    });

    [Fact]
    public Task Cancel_closes_with_false() => Ui.Run(async () =>
    {
        var window = DialogDriver.Window();
        var (dialog, _) = Build();
        var result = dialog.ShowAsync(window);

        DialogDriver.Click(dialog.FindControl<Button>("CancelButton")!);

        Assert.False(await result.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Null(InlineDialog.OpenIn(window));
    });

    [Fact]
    public Task The_primary_button_closes_with_true() => Ui.Run(async () =>
    {
        var window = DialogDriver.Window();
        var (dialog, _) = Build();
        var result = dialog.ShowAsync(window);

        DialogDriver.Click(dialog.FindControl<Button>("PrimaryButton")!);

        Assert.True(await result.WaitAsync(TimeSpan.FromSeconds(5)));
    });

    [Theory]
    [InlineData(Key.Enter, true)]
    [InlineData(Key.Escape, false)]
    public Task Enter_accepts_and_esc_cancels(Key key, bool expected) => Ui.Run(async () =>
    {
        var window = DialogDriver.Window();
        var (dialog, box) = Build();
        var result = dialog.ShowAsync(window);

        DialogDriver.Press(box, key);

        Assert.Equal(expected, await result.WaitAsync(TimeSpan.FromSeconds(5)));
    });

    [Fact]
    public Task A_failed_accept_check_keeps_the_dialog_open() => Ui.Run(() =>
    {
        var window = DialogDriver.Window();
        var (dialog, _) = Build();
        dialog.Accept = () => Task.FromResult(false);
        var result = dialog.ShowAsync(window);

        DialogDriver.Click(dialog.FindControl<Button>("PrimaryButton")!);

        Assert.False(result.IsCompleted);
        Assert.Same(dialog, InlineDialog.OpenIn(window));
    });

    [Fact]
    public Task A_disabled_primary_ignores_enter() => Ui.Run(() =>
    {
        var window = DialogDriver.Window();
        var (dialog, box) = Build();
        dialog.IsPrimaryEnabled = false;
        var result = dialog.ShowAsync(window);

        DialogDriver.Press(box, Key.Enter);

        Assert.False(result.IsCompleted);
        Assert.False(dialog.FindControl<Button>("PrimaryButton")!.IsEnabled);
    });

    [Fact]
    public Task An_error_shows_under_the_content() => Ui.Run(() =>
    {
        var window = DialogDriver.Window();
        var (dialog, _) = Build();
        _ = dialog.ShowAsync(window);

        dialog.ShowError("Taken");

        var error = dialog.FindControl<TextBlock>("ErrorText")!;
        Assert.True(error.IsVisible);
        Assert.Equal("Taken", error.Text);
    });
}
