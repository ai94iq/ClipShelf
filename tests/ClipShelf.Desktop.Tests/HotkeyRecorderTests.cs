using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform;

namespace ClipShelf.Desktop.Tests;

public sealed class HotkeyRecorderTests
{
    [Theory]
    [InlineData(Key.V, KeyModifiers.Control | KeyModifiers.Shift, HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x56u)]
    [InlineData(Key.F5, KeyModifiers.Meta | KeyModifiers.Alt, HotkeyModifiers.Win | HotkeyModifiers.Alt, 0x74u)]
    [InlineData(Key.D3, KeyModifiers.Control, HotkeyModifiers.Control, 0x33u)]
    [InlineData(Key.F24, KeyModifiers.Alt, HotkeyModifiers.Alt, 0x87u)]
    public void A_chord_becomes_a_gesture(Key key, KeyModifiers modifiers, HotkeyModifiers expected, uint virtualKey) =>
        Assert.Equal(new HotkeyGesture(expected, virtualKey), HotkeyCapture.TryCapture(key, modifiers));

    [Theory]
    [InlineData(Key.LeftCtrl, KeyModifiers.Control)]     // a modifier alone keeps waiting
    [InlineData(Key.LWin, KeyModifiers.Meta)]
    [InlineData(Key.V, KeyModifiers.None)]               // no modifier
    [InlineData(Key.Space, KeyModifiers.Control)]        // unsupported main key
    public void Anything_else_is_not_a_gesture(Key key, KeyModifiers modifiers) =>
        Assert.Null(HotkeyCapture.TryCapture(key, modifiers));

    private static Window Host(Control page)
    {
        var window = new Window { Content = page, Width = 900, Height = 700 };
        window.Show();
        return window;
    }

    private static void Press(Control target, Key key, KeyModifiers modifiers = KeyModifiers.None) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers });

    private static Button StartRecording(SettingsKit kit)
    {
        var button = kit.Page.FindControl<Button>("HotkeyButton")!;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        return button;
    }

    [Fact]
    public Task Clicking_the_hotkey_button_asks_for_a_chord() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        StartRecording(kit);

        Assert.True(kit.Page.FindControl<Control>("HotkeyPrompt")!.IsVisible);
        Assert.False(kit.Page.FindControl<Control>("HotkeyText")!.IsVisible);
    });

    [Fact]
    public Task A_recorded_chord_becomes_the_hotkey() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);
        var button = StartRecording(kit);

        Press(button, Key.LeftCtrl, KeyModifiers.Control);
        Press(button, Key.K, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.Equal(new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x4B), kit.Settings.Current.Hotkey);
        Assert.False(kit.Page.FindControl<Control>("HotkeyPrompt")!.IsVisible);
        Assert.Equal("Ctrl+Shift+K", kit.Page.FindControl<TextBlock>("HotkeyText")!.Text);
    });

    [Fact]
    public Task Esc_stops_recording_and_keeps_the_hotkey() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        var before = kit.Settings.Current.Hotkey;
        Host(kit.Page);
        var button = StartRecording(kit);

        Press(button, Key.Escape);

        Assert.Equal(before, kit.Settings.Current.Hotkey);
        Assert.False(kit.Page.FindControl<Control>("HotkeyPrompt")!.IsVisible);
    });

    [Fact]
    public Task Keys_without_recording_change_nothing() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        var before = kit.Settings.Current.Hotkey;
        Host(kit.Page);

        Press(kit.Page.FindControl<Button>("HotkeyButton")!, Key.K, KeyModifiers.Control);

        Assert.Equal(before, kit.Settings.Current.Hotkey);
    });

    [Fact]
    public Task A_taken_hotkey_shows_the_warning_with_alternatives() => Ui.Run(() =>
    {
        var kit = new SettingsKit(hotkeyRegistered: false);
        Host(kit.Page);

        Assert.True(kit.Page.FindControl<Control>("HotkeyWarning")!.IsVisible);
        var alternatives = kit.Page.FindControl<ItemsControl>("HotkeyAlternatives")!
            .GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(kit.ViewModel.HotkeyAlternatives.Count, alternatives.Count);

        var first = kit.ViewModel.HotkeyAlternatives[0].Value;
        alternatives[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(first, kit.Settings.Current.Hotkey);
    });

    [Fact]
    public Task A_registered_hotkey_shows_no_warning() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        Host(kit.Page);

        Assert.False(kit.Page.FindControl<Control>("HotkeyWarning")!.IsVisible);
    });
}
