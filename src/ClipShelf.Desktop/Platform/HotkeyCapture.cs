using Avalonia.Input;
using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform;

// Turns a key press in the Settings recorder into a global hotkey. The main key maps onto the
// Win32 virtual-key code HotkeyGesture stores (A-Z, 0-9 and F1-F24 have contiguous runs in
// both); a modifier alone or a key the hotkey cannot use gives null, so the recorder waits.
internal static class HotkeyCapture
{
    public static HotkeyGesture? TryCapture(Key key, KeyModifiers keyModifiers)
    {
        uint? virtualKey = key switch
        {
            >= Key.A and <= Key.Z => 0x41 + (uint)(key - Key.A),
            >= Key.D0 and <= Key.D9 => 0x30 + (uint)(key - Key.D0),
            >= Key.F1 and <= Key.F24 => 0x70 + (uint)(key - Key.F1),
            _ => null,
        };
        if (virtualKey is null) return null;

        var modifiers = HotkeyModifiers.None;
        if (keyModifiers.HasFlag(KeyModifiers.Control)) modifiers |= HotkeyModifiers.Control;
        if (keyModifiers.HasFlag(KeyModifiers.Alt)) modifiers |= HotkeyModifiers.Alt;
        if (keyModifiers.HasFlag(KeyModifiers.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (keyModifiers.HasFlag(KeyModifiers.Meta)) modifiers |= HotkeyModifiers.Win;

        var gesture = new HotkeyGesture(modifiers, virtualKey.Value);
        return gesture.IsValid ? gesture : null;
    }
}
