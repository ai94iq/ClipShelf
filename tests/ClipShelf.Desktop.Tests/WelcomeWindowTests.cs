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

        Assert.Contains(HotkeyText.Display(new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x56)), window.FindControl<TextBlock>("HotkeyText")!.Text);
    });

    // GNOME without the AppIndicator extension: there is no tray icon to point at.
    [Fact]
    public Task Without_a_tray_the_welcome_says_how_to_reach_the_app() => Ui.Run(() =>
    {
        var withTray = new WelcomeWindow(Settings()).FindControl<TextBlock>("TrayText")!.Text;
        var without = new WelcomeWindow(Settings(), hasTray: false).FindControl<TextBlock>("TrayText")!.Text;

        Assert.NotEqual(withTray, without);
        Assert.Contains("clipshelf --history", without);
    });

    // Wayland: keys pressed in other apps never reach the grab, so "press it anywhere" would be wrong.
    [Fact]
    public Task Without_a_global_hotkey_the_welcome_explains_the_binding() => Ui.Run(() =>
    {
        var text = new WelcomeWindow(Settings(), globalHotkey: false).FindControl<TextBlock>("HotkeyText")!.Text;

        Assert.Contains("clipshelf --toggle", text);
    });

    // macOS: pasting for the user needs the Accessibility permission, which macOS asks for once.
    [Fact]
    public Task On_a_mac_the_welcome_explains_the_accessibility_prompt() => Ui.Run(() =>
    {
        var plain = new WelcomeWindow(Settings()).FindControl<TextBlock>("AccessibilityText")!;
        var mac = new WelcomeWindow(Settings(), explainAccessibility: true).FindControl<TextBlock>("AccessibilityText")!;

        Assert.False(plain.IsVisible);
        Assert.True(mac.IsVisible);
        Assert.Contains("Accessibility", mac.Text);
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
