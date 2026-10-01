namespace ClipShelf.Core.Input;

// Modifier bits match the Win32 MOD_* flags, so registering a hotkey is a plain cast.
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
}

// A global shortcut: at least one modifier plus one supported main key (A-Z, 0-9, F1-F24).
public sealed record HotkeyGesture(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public bool IsValid =>
        Modifiers != HotkeyModifiers.None && HotkeyText.IsSupportedKey(VirtualKey);
}
