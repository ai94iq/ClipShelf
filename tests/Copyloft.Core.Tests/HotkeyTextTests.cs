using Copyloft.Core.Input;

namespace Copyloft.Core.Tests;

public sealed class HotkeyTextTests
{
    // On a Mac the labels follow Apple's names and order; what settings.json stores does not change.
    [Fact]
    public void Display_on_a_mac_uses_apples_names_and_order() =>
        Assert.Equal("Control+Option+Shift+Cmd+V",
            HotkeyText.Display(new HotkeyGesture(HotkeyModifiers.Win | HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x56), mac: true));

    [Fact]
    public void Display_elsewhere_matches_format()
    {
        var gesture = new HotkeyGesture(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56);

        Assert.Equal(HotkeyText.Format(gesture), HotkeyText.Display(gesture, mac: false));
    }

    [Fact]
    public void Format_uses_windows_order_and_the_ctrl_name() =>
        Assert.Equal("Win+Ctrl+Shift+V",
            HotkeyText.Format(new HotkeyGesture(
                HotkeyModifiers.Win | HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x56)));

    [Theory]
    [InlineData("Win+Shift+V", HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56u)]
    [InlineData("Ctrl+Alt+C", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43u)]
    [InlineData("ctrl+shift+f12", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x7Bu)]
    public void TryParse_reads_a_gesture(string text, HotkeyModifiers modifiers, uint key)
    {
        Assert.True(HotkeyText.TryParse(text, out var gesture));

        Assert.Equal(modifiers, gesture!.Modifiers);
        Assert.Equal(key, gesture.VirtualKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("V")]
    [InlineData("Win")]
    [InlineData("Win+Foo")]
    [InlineData("Shift+Escape")]
    [InlineData("Win+Shift")]
    public void TryParse_rejects_invalid_text(string? text) =>
        Assert.False(HotkeyText.TryParse(text, out _));

    [Fact]
    public void Format_and_TryParse_round_trip()
    {
        var gesture = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x41);

        Assert.True(HotkeyText.TryParse(HotkeyText.Format(gesture), out var parsed));
        Assert.Equal(gesture, parsed);
    }

    [Fact]
    public void IsValid_requires_a_modifier_and_a_supported_key()
    {
        Assert.False(new HotkeyGesture(HotkeyModifiers.None, 0x56).IsValid);
        Assert.False(new HotkeyGesture(HotkeyModifiers.Win, 0x1B).IsValid);
        Assert.True(new HotkeyGesture(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56).IsValid);
    }
}
