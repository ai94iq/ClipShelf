using ClipShelf.Core.Input;

namespace ClipShelf.App.Services;

// The decision half of global-hotkey registration: what to try, and what stays registered when
// the chosen shortcut is taken. The P/Invoke stays in GlobalHotkey, so this part is testable.
public sealed class HotkeyRegistrationState
{
    public HotkeyGesture? Registered { get; private set; }

    // Applies the desired shortcut through register; when it fails, the previously registered one
    // is restored. Returns whether the desired shortcut is the active one.
    public bool Apply(HotkeyGesture desired, Func<HotkeyGesture, bool> register)
    {
        if (register(desired))
        {
            Registered = desired;
            return true;
        }

        if (Registered is null) return false;

        if (register(Registered)) return false;

        Registered = null;                    // the fallback failed too; nothing is active
        return false;
    }
}
