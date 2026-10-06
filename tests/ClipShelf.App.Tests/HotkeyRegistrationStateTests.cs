using ClipShelf.App.Services;
using ClipShelf.Core.Input;

namespace ClipShelf.App.Tests;

// The decision half of hotkey registration; GlobalHotkey owns the P/Invoke.
public sealed class HotkeyRegistrationStateTests
{
    private static HotkeyGesture Gesture(HotkeyModifiers modifiers, uint key) => new(modifiers, key);

    [Fact]
    public void A_successful_change_registers_the_new_shortcut()
    {
        var state = new HotkeyRegistrationState();
        var wanted = Gesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);

        var applied = state.Apply(wanted, _ => true);

        Assert.True(applied);
        Assert.Equal(wanted, state.Registered);
    }

    [Fact]
    public void A_taken_shortcut_keeps_the_previous_one_registered()
    {
        var state = new HotkeyRegistrationState();
        var old = Gesture(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56);
        var wanted = Gesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);
        state.Apply(old, _ => true);
        var attempts = new List<HotkeyGesture>();

        var applied = state.Apply(wanted, gesture =>
        {
            attempts.Add(gesture);
            return gesture == old;
        });

        Assert.False(applied);
        Assert.Equal(old, state.Registered);
        Assert.Equal(new[] { wanted, old }, attempts);
    }

    [Fact]
    public void A_first_registration_failure_leaves_nothing_registered()
    {
        var state = new HotkeyRegistrationState();

        var applied = state.Apply(Gesture(HotkeyModifiers.Control, 0x43), _ => false);

        Assert.False(applied);
        Assert.Null(state.Registered);
    }

    [Fact]
    public void A_failed_fallback_clears_the_registered_state()
    {
        var state = new HotkeyRegistrationState();
        var old = Gesture(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56);
        state.Apply(old, _ => true);

        var applied = state.Apply(Gesture(HotkeyModifiers.Control, 0x43), _ => false);

        Assert.False(applied);
        Assert.Null(state.Registered);
    }
}
