using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform.MacOS;

// HotkeyGesture keeps Win32 virtual keys and MOD_* bits; Carbon's RegisterEventHotKey takes the
// Mac key codes (kVK_*, which follow the US key positions) and its own modifier masks.
internal static class MacKeys
{
    private const uint CmdKey = 0x0100, ShiftKey = 0x0200, OptionKey = 0x0800, ControlKey = 0x1000;

    // A-Z in alphabetical order.
    private static readonly uint[] Letters =
    [
        0x00, 0x0B, 0x08, 0x02, 0x0E, 0x03, 0x05, 0x04, 0x22, 0x26, 0x28, 0x25, 0x2E,
        0x2D, 0x1F, 0x23, 0x0C, 0x0F, 0x01, 0x11, 0x20, 0x09, 0x0D, 0x07, 0x10, 0x06,
    ];

    // 0-9.
    private static readonly uint[] Digits = [0x1D, 0x12, 0x13, 0x14, 0x15, 0x17, 0x16, 0x1A, 0x1C, 0x19];

    // F1-F20; Mac keyboards stop at F20.
    private static readonly uint[] FunctionKeys =
    [
        0x7A, 0x78, 0x63, 0x76, 0x60, 0x61, 0x62, 0x64, 0x65, 0x6D,
        0x67, 0x6F, 0x69, 0x6B, 0x71, 0x6A, 0x40, 0x4F, 0x50, 0x5A,
    ];

    public static uint? KeyCode(uint virtualKey) => virtualKey switch
    {
        >= 0x41 and <= 0x5A => Letters[virtualKey - 0x41],
        >= 0x30 and <= 0x39 => Digits[virtualKey - 0x30],
        >= 0x70 and <= 0x83 => FunctionKeys[virtualKey - 0x70],
        _ => null,
    };

    public static uint Modifiers(HotkeyModifiers modifiers) =>
        (modifiers.HasFlag(HotkeyModifiers.Win) ? CmdKey : 0)
        | (modifiers.HasFlag(HotkeyModifiers.Shift) ? ShiftKey : 0)
        | (modifiers.HasFlag(HotkeyModifiers.Alt) ? OptionKey : 0)
        | (modifiers.HasFlag(HotkeyModifiers.Control) ? ControlKey : 0);
}
