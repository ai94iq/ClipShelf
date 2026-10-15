using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform.Linux;

// HotkeyGesture keeps Win32 virtual keys and MOD_* bits; X11 grabs keysyms with its own masks.
internal static class X11Keys
{
    internal const uint ShiftMask = 1, LockMask = 2, ControlMask = 4, Mod1Mask = 8, Mod2Mask = 0x10, Mod4Mask = 0x40;
    private const uint Meaningful = ShiftMask | ControlMask | Mod1Mask | Mod4Mask;

    // A-Z grab the lowercase keysym, 0-9 share their codes, F1-F24 run from XK_F1.
    public static uint Keysym(uint virtualKey) => virtualKey switch
    {
        >= 0x41 and <= 0x5A => 0x61 + (virtualKey - 0x41),
        >= 0x30 and <= 0x39 => virtualKey,
        >= 0x70 and <= 0x87 => 0xFFBE + (virtualKey - 0x70),
        _ => 0,
    };

    // Alt is Mod1 and the Windows key is Super (Mod4) on every common layout.
    public static uint Mask(HotkeyModifiers modifiers) =>
        (modifiers.HasFlag(HotkeyModifiers.Shift) ? ShiftMask : 0)
        | (modifiers.HasFlag(HotkeyModifiers.Control) ? ControlMask : 0)
        | (modifiers.HasFlag(HotkeyModifiers.Alt) ? Mod1Mask : 0)
        | (modifiers.HasFlag(HotkeyModifiers.Win) ? Mod4Mask : 0);

    // X delivers a grab only for the exact modifier state, so Caps Lock and Num Lock variants are
    // grabbed too.
    public static uint[] GrabMasks(uint mask) => [mask, mask | LockMask, mask | Mod2Mask, mask | LockMask | Mod2Mask];

    public static bool Matches(uint state, uint mask) => (state & Meaningful) == mask;
}
