using Avalonia.Input;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform.Windows;

namespace ClipShelf.Desktop.Tests;

// The recorder maps Avalonia keys to Win32 virtual keys with an explicit table: Avalonia Key
// values do not equal VK codes, so a cast would bind the wrong shortcut.
public sealed class DesktopHotkeyTests
{
    [Theory]
    [InlineData(Key.A, 0x41)]
    [InlineData(Key.M, 0x4D)]
    [InlineData(Key.Z, 0x5A)]
    public void Letters_map_to_their_virtual_keys(Key key, uint expected)
    {
        var gesture = DesktopHotkey.TryCapture(key, KeyModifiers.Control);

        Assert.Equal(new HotkeyGesture(HotkeyModifiers.Control, expected), gesture);
    }

    [Theory]
    [InlineData(Key.D0, 0x30)]
    [InlineData(Key.D5, 0x35)]
    [InlineData(Key.D9, 0x39)]
    public void Digits_map_to_their_virtual_keys(Key key, uint expected)
    {
        var gesture = DesktopHotkey.TryCapture(key, KeyModifiers.Control);

        Assert.Equal(new HotkeyGesture(HotkeyModifiers.Control, expected), gesture);
    }

    [Theory]
    [InlineData(Key.F1, 0x70)]
    [InlineData(Key.F5, 0x74)]
    [InlineData(Key.F12, 0x7B)]
    public void Function_keys_map_to_their_virtual_keys(Key key, uint expected)
    {
        var gesture = DesktopHotkey.TryCapture(key, KeyModifiers.Control);

        Assert.Equal(new HotkeyGesture(HotkeyModifiers.Control, expected), gesture);
    }

    [Fact]
    public void Modifiers_follow_the_pressed_flags_with_meta_as_win()
    {
        var gesture = DesktopHotkey.TryCapture(
            Key.V, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift | KeyModifiers.Meta);

        Assert.Equal(
            new HotkeyGesture(
                HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Win,
                0x56),
            gesture);
    }

    [Theory]
    [InlineData(Key.LeftCtrl, KeyModifiers.Control)]
    [InlineData(Key.RightShift, KeyModifiers.Shift)]
    [InlineData(Key.LeftAlt, KeyModifiers.Alt)]
    [InlineData(Key.LWin, KeyModifiers.Meta)]
    public void Modifier_only_input_keeps_waiting(Key key, KeyModifiers modifiers)
    {
        Assert.Null(DesktopHotkey.TryCapture(key, modifiers));
    }

    [Fact]
    public void A_key_with_no_modifiers_keeps_waiting()
    {
        Assert.Null(DesktopHotkey.TryCapture(Key.V, KeyModifiers.None));
    }
}
