using ClipShelf.Core.Input;

namespace ClipShelf.App.Services;

// What happened to the last hotkey registration attempt: the settings page warns when the
// chosen shortcut could not be registered and names the one that is still active.
public interface IHotkeyRegistration
{
    bool IsCurrentHotkeyRegistered { get; }

    // The shortcut that is actually active right now (the previous one after a failed change).
    HotkeyGesture? RegisteredHotkey { get; }
}
