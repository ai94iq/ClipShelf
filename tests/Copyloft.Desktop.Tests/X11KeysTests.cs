using Copyloft.Core.Input;
using Copyloft.Desktop.Platform.Linux;

namespace Copyloft.Desktop.Tests;

public sealed class X11KeysTests
{
    [Theory]
    [InlineData(0x41u, 0x61u)]   // A -> XK_a
    [InlineData(0x56u, 0x76u)]   // V -> XK_v
    [InlineData(0x5Au, 0x7Au)]   // Z -> XK_z
    [InlineData(0x30u, 0x30u)]   // 0 -> XK_0
    [InlineData(0x39u, 0x39u)]   // 9 -> XK_9
    [InlineData(0x70u, 0xFFBEu)] // F1 -> XK_F1
    [InlineData(0x87u, 0xFFD5u)] // F24 -> XK_F24
    public void Supported_keys_map_to_their_keysyms(uint virtualKey, uint keysym) =>
        Assert.Equal(keysym, X11Keys.Keysym(virtualKey));

    [Fact]
    public void Win_shift_maps_to_super_and_shift() =>
        Assert.Equal(X11Keys.Mod4Mask | X11Keys.ShiftMask, X11Keys.Mask(HotkeyModifiers.Win | HotkeyModifiers.Shift));

    [Fact]
    public void Control_alt_maps_to_control_and_mod1() =>
        Assert.Equal(X11Keys.ControlMask | X11Keys.Mod1Mask, X11Keys.Mask(HotkeyModifiers.Control | HotkeyModifiers.Alt));

    [Fact]
    public void Caps_and_num_lock_still_match_but_an_extra_modifier_does_not()
    {
        var mask = X11Keys.ControlMask | X11Keys.ShiftMask;

        Assert.All(X11Keys.GrabMasks(mask), state => Assert.True(X11Keys.Matches(state, mask)));
        Assert.False(X11Keys.Matches(mask | X11Keys.Mod1Mask, mask));
    }
}
