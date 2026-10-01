using ClipShelf.Core.Input;
using Windows.System;

namespace ClipShelf.App.Platform;

// Builds a hotkey from the key the user pressed and the modifiers held at that moment.
internal static class HotkeyCapture
{
    public static HotkeyGesture? TryCapture(VirtualKey key)
    {
        if (IsModifier(key)) return null;

        var modifiers = HotkeyModifiers.None;
        if (IsDown(VirtualKey.Control)) modifiers |= HotkeyModifiers.Control;
        if (IsDown(VirtualKey.Menu)) modifiers |= HotkeyModifiers.Alt;
        if (IsDown(VirtualKey.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows)) modifiers |= HotkeyModifiers.Win;

        var gesture = new HotkeyGesture(modifiers, (uint)key);
        return gesture.IsValid ? gesture : null;
    }

    private static bool IsModifier(VirtualKey key) =>
        key is VirtualKey.Shift or VirtualKey.Control or VirtualKey.Menu
            or VirtualKey.LeftWindows or VirtualKey.RightWindows;

    private static bool IsDown(VirtualKey key) =>
        (NativeMethods.GetKeyState((int)key) & 0x8000) != 0;
}
