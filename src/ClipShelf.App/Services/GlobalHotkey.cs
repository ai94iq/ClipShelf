using ClipShelf.App.Platform;

namespace ClipShelf.App.Services;

// Registers one global hotkey on the shared message window and raises an event when it fires.
public sealed class GlobalHotkey : IDisposable
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

    public bool Register(uint modifiers, uint virtualKey)
    {
        var registered = NativeMethods.RegisterHotKey(MessagePump.Handle, Id, modifiers, virtualKey);
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
