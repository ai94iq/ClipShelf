using Avalonia.Controls;
using Avalonia.Interactivity;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Shell;
using ClipShelf.Presentation.Hosting;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

public sealed class WelcomeWindowTests
{
    private static SettingsService Settings(bool welcomeShown = false) =>
        new(Substitute.For<ISettingsStore>(), new AppSettings
        {
            WelcomeShown = welcomeShown,
            Hotkey = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x56),
        });

    [Fact]
    public Task The_welcome_names_the_saved_hotkey() => Ui.Run(() =>
    {
        var window = new WelcomeWindow(Settings());

        Assert.Contains("Ctrl+Alt+V", window.FindControl<TextBlock>("HotkeyText")!.Text);
    });

    [Fact]
    public Task Got_it_closes_the_window() => Ui.Run(() =>
    {
        var window = new WelcomeWindow(Settings());
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();

        window.FindControl<Button>("OkButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.True(closed);
    });

    [Fact]
    public Task The_first_run_shows_it_once_and_remembers() => Ui.Run(() =>
    {
        var settings = Settings();
        WelcomeWindow? shown = null;

        WelcomeWindow.ShowOnFirstRun(settings, () => shown = new WelcomeWindow(settings));

        Assert.True(shown!.IsVisible);
        Assert.True(settings.Current.WelcomeShown);
    });

    [Fact]
    public Task Later_runs_skip_it() => Ui.Run(() =>
    {
        var created = false;

        WelcomeWindow.ShowOnFirstRun(Settings(welcomeShown: true), () => { created = true; return null!; });

        Assert.False(created);
    });
}
