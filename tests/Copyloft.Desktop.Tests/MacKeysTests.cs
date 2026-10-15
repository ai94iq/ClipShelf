using Copyloft.Core.Input;
using Copyloft.Desktop.Platform.MacOS;

namespace Copyloft.Desktop.Tests;

public sealed class MacKeysTests
{
    // kVK_ANSI_* and kVK_F* from HIToolbox Events.h: the codes follow the US key positions,
    // not the alphabet.
    [Theory]
    [InlineData(0x41u, 0x00u)]   // A
    [InlineData(0x56u, 0x09u)]   // V
    [InlineData(0x5Au, 0x06u)]   // Z
    [InlineData(0x30u, 0x1Du)]   // 0
    [InlineData(0x35u, 0x17u)]   // 5
    [InlineData(0x39u, 0x19u)]   // 9
    [InlineData(0x70u, 0x7Au)]   // F1
    [InlineData(0x7Bu, 0x6Fu)]   // F12
    [InlineData(0x83u, 0x5Au)]   // F20
    public void Supported_keys_map_to_their_mac_key_codes(uint virtualKey, uint keyCode) =>
        Assert.Equal(keyCode, MacKeys.KeyCode(virtualKey));

    [Fact]
    public void Keys_a_mac_keyboard_lacks_have_no_code() =>
        Assert.Null(MacKeys.KeyCode(0x84));   // F21

    // The Windows key is Command on a Mac.
    [Fact]
    public void Win_shift_maps_to_command_shift() =>
        Assert.Equal(0x0100u | 0x0200u, MacKeys.Modifiers(HotkeyModifiers.Win | HotkeyModifiers.Shift));

    [Fact]
    public void Control_alt_maps_to_control_option() =>
        Assert.Equal(0x1000u | 0x0800u, MacKeys.Modifiers(HotkeyModifiers.Control | HotkeyModifiers.Alt));
}
