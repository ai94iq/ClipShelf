using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform.Linux;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the gesture mapping: pure logic, no display.
public sealed class X11HotkeyTests
{
    [Fact]
    public void ToMask_MapsPortableModifiers()
    {
        Assert.Equal(8u, X11Hotkey.ToMask(HotkeyModifiers.Alt));
        Assert.Equal(9u, X11Hotkey.ToMask(HotkeyModifiers.Alt | HotkeyModifiers.Shift));
        Assert.Equal(68u, X11Hotkey.ToMask(HotkeyModifiers.Win | HotkeyModifiers.Control));
        Assert.Equal(0u, X11Hotkey.ToMask(HotkeyModifiers.None));
    }

    [Fact]
    public void ToKeysymName_MapsSupportedKeys()
    {
        Assert.Equal("v", X11Hotkey.ToKeysymName(0x56));
        Assert.Equal("5", X11Hotkey.ToKeysymName(0x35));
        Assert.Equal("F5", X11Hotkey.ToKeysymName(0x74));
        Assert.Equal("F24", X11Hotkey.ToKeysymName(0x87));
    }

    [Fact]
    public void ToKeysymName_RejectsUnsupportedKeys() =>
        Assert.Null(X11Hotkey.ToKeysymName(0x99));

    [Fact]
    public void GrabMasks_CoversLockStates()
    {
        var masks = X11Hotkey.GrabMasks(9u);
        Assert.Equal(4, masks.Length);
        Assert.Equal(masks.Distinct().Count(), masks.Length);
        Assert.All(masks, mask => Assert.Equal(9u, mask & 9u));
    }
}
