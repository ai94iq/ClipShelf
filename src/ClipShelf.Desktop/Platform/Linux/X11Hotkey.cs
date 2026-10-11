using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform.Linux;

// Pure mapping from portable hotkey gestures to X11 masks and keysym names.
// Lock states (Caps, NumLock) are grabbed as separate combinations by the
// service; they never come from the gesture itself.
public static class X11Hotkey
{
    internal const uint ShiftMask = 1u << 0;
    internal const uint LockMask = 1u << 1;
    internal const uint ControlMask = 1u << 2;
    internal const uint Mod1Mask = 1u << 3;
    internal const uint Mod2Mask = 1u << 4;
    internal const uint Mod4Mask = 1u << 6;

    internal const int AnyKey = 0;
    internal const int AnyModifier = 1 << 15;

    internal const int KeyPress = 2;

    internal const int GrabKeyRequest = 33;
    internal const int BadAccess = 10;

    public static uint ToMask(HotkeyModifiers modifiers)
    {
        var mask = 0u;
        if (modifiers.HasFlag(HotkeyModifiers.Shift)) mask |= ShiftMask;
        if (modifiers.HasFlag(HotkeyModifiers.Control)) mask |= ControlMask;
        if (modifiers.HasFlag(HotkeyModifiers.Alt)) mask |= Mod1Mask;
        if (modifiers.HasFlag(HotkeyModifiers.Win)) mask |= Mod4Mask;
        return mask;
    }

    // A-Z and 0-9 as literal keysyms, F1-F24 by name; anything else has no X11
    // equivalent and fails registration loudly.
    public static string? ToKeysymName(uint virtualKey)
    {
        if (virtualKey is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A)
            return ((char)virtualKey).ToString().ToLowerInvariant();
        if (virtualKey is >= 0x70 and <= 0x87)
            return "F" + (virtualKey - 0x6F);
        return null;
    }

    // The base mask plus every Caps/NumLock combination, so the shortcut fires
    // regardless of lock state.
    public static uint[] GrabMasks(uint mask) =>
        [mask, mask | Mod2Mask, mask | LockMask, mask | Mod2Mask | LockMask];
}
