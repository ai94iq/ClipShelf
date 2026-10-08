using ClipShelf.App.Platform;
using ClipShelf.Core.Input;

namespace ClipShelf.App.Services;

// Registers one global hotkey on the shared message window and raises an event when it fires.
public sealed class GlobalHotkey : IGlobalHotkey
{
    private const int Id = 0x0C15;

    private readonly ILogger<GlobalHotkey> _log;

    public GlobalHotkey(ILogger<GlobalHotkey> log)
    {
        _log = log;
        MessagePump.EnsureCreated();
        MessagePump.HotkeyPressed += OnHotkeyPressed;
    }

    public event EventHandler? Pressed;

    public bool Register(HotkeyGesture gesture)
    {
        // Re-registering under the same id needs a release first; this is a no-op before the first one.
        NativeMethods.UnregisterHotKey(MessagePump.Handle, Id);
        var registered = NativeMethods.RegisterHotKey(
            MessagePump.Handle, Id, (uint)gesture.Modifiers | NativeMethods.ModNoRepeat, (uint)gesture.VirtualKey);
        if (!registered) _log.LogWarning("Registering the global hotkey failed; another app may own it");
        return registered;
    }

    public void Dispose()
    {
        NativeMethods.UnregisterHotKey(MessagePump.Handle, Id);
        MessagePump.HotkeyPressed -= OnHotkeyPressed;
    }

    private void OnHotkeyPressed(int id)
    {
        if (id == Id) Pressed?.Invoke(this, EventArgs.Empty);
    }
}
