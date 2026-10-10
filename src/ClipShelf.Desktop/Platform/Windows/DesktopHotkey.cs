using Avalonia.Input;
using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform.Windows;

// Builds a hotkey from the key the settings recorder captured and the modifiers held with it.
// The table is explicit: Avalonia Key values do not equal Win32 virtual-key codes, so the main
// key is never a cast. Modifier keys alone (and anything outside the table) return null, which
// keeps the recorder waiting for a main key.
public static class DesktopHotkey
{
    public static HotkeyGesture? TryCapture(Key key, KeyModifiers modifiers)
    {
        if (!ToVirtualKey(key, out var virtualKey)) return null;

        var gestureModifiers = HotkeyModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control)) gestureModifiers |= HotkeyModifiers.Control;
        if (modifiers.HasFlag(KeyModifiers.Alt)) gestureModifiers |= HotkeyModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Shift)) gestureModifiers |= HotkeyModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Meta)) gestureModifiers |= HotkeyModifiers.Win;

        var gesture = new HotkeyGesture(gestureModifiers, virtualKey);
        return gesture.IsValid ? gesture : null;
    }

    private static bool ToVirtualKey(Key key, out uint virtualKey)
    {
        virtualKey = key switch
        {
            Key.A => 0x41,
            Key.B => 0x42,
            Key.C => 0x43,
            Key.D => 0x44,
            Key.E => 0x45,
            Key.F => 0x46,
            Key.G => 0x47,
            Key.H => 0x48,
            Key.I => 0x49,
            Key.J => 0x4A,
            Key.K => 0x4B,
            Key.L => 0x4C,
            Key.M => 0x4D,
            Key.N => 0x4E,
            Key.O => 0x4F,
            Key.P => 0x50,
            Key.Q => 0x51,
            Key.R => 0x52,
            Key.S => 0x53,
            Key.T => 0x54,
            Key.U => 0x55,
            Key.V => 0x56,
            Key.W => 0x57,
            Key.X => 0x58,
            Key.Y => 0x59,
            Key.Z => 0x5A,
            Key.D0 => 0x30,
            Key.D1 => 0x31,
            Key.D2 => 0x32,
            Key.D3 => 0x33,
            Key.D4 => 0x34,
            Key.D5 => 0x35,
            Key.D6 => 0x36,
            Key.D7 => 0x37,
            Key.D8 => 0x38,
            Key.D9 => 0x39,
            Key.F1 => 0x70,
            Key.F2 => 0x71,
            Key.F3 => 0x72,
            Key.F4 => 0x73,
            Key.F5 => 0x74,
            Key.F6 => 0x75,
            Key.F7 => 0x76,
            Key.F8 => 0x77,
            Key.F9 => 0x78,
            Key.F10 => 0x79,
            Key.F11 => 0x7A,
            Key.F12 => 0x7B,
            _ => 0,
        };
        return virtualKey != 0;
    }
}
