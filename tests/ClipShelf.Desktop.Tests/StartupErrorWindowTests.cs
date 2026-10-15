using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Shell;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Localization;

namespace ClipShelf.Desktop.Tests;

// A failed start used to end the app with nothing on screen (a denied Keychain request on macOS).
public sealed class StartupErrorWindowTests
{
    [Fact]
    public void A_keyring_denial_says_to_unlock_or_allow_access_even_when_wrapped() =>
        Assert.Equal(Tr.Get("Startup_KeyringDenied"), StartupErrorWindow.Message(
            new AggregateException(new KeyringUnavailableException("denied"))));

    [Fact]
    public void Any_other_failure_gets_the_plain_message() =>
        Assert.Equal(Tr.Get("Startup_Failed"), StartupErrorWindow.Message(new IOException("disk")));

    [Fact]
    public Task The_window_names_the_log_and_ok_closes_it() => Ui.Run(() =>
    {
        var window = new StartupErrorWindow(new KeyringUnavailableException("denied"));
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();

        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text!.Contains(AppPaths.Logs));
        window.GetVisualDescendants().OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(closed);
    });
}
