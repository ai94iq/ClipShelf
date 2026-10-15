using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClipShelf.Core.Input;
using static ClipShelf.Desktop.Platform.MacOS.MacNative;

namespace ClipShelf.Desktop.Platform.MacOS;

// One Carbon hotkey (RegisterEventHotKey needs no Accessibility permission). Carbon delivers the
// press on the main run loop, which is Avalonia's UI thread.
[SupportedOSPlatform("macos")]
public sealed class MacGlobalHotkey(ILogger<MacGlobalHotkey> log) : IGlobalHotkey
{
    private const uint KeyboardClass = 0x6B657962;   // 'keyb'
    private const uint HotKeyPressed = 5;            // kEventHotKeyPressed
    private const uint Signature = 0x434C5053;       // 'CLPS'

    private delegate int EventHandlerProc(IntPtr callRef, IntPtr evt, IntPtr userData);

    // Held statically so Carbon never calls a collected delegate.
    private static readonly EventHandlerProc Handler = OnHotKey;
    private static MacGlobalHotkey? _current;
    private IntPtr _hotKey;
    private bool _installed;

    public event EventHandler? Pressed;

    public bool Register(HotkeyGesture gesture)
    {
        _current = this;
        if (!_installed)
        {
            var spec = new EventTypeSpec { EventClass = KeyboardClass, EventKind = HotKeyPressed };
            InstallEventHandler(GetApplicationEventTarget(), Marshal.GetFunctionPointerForDelegate(Handler), 1, ref spec, IntPtr.Zero, out _);
            _installed = true;
        }

        Unregister();
        if (MacKeys.KeyCode(gesture.VirtualKey) is not { } keyCode) return false;

        var status = RegisterEventHotKey(
            keyCode, MacKeys.Modifiers(gesture.Modifiers), new EventHotKeyId { Signature = Signature, Id = 1 },
            GetApplicationEventTarget(), 0, out _hotKey);
        if (status == 0) return true;

        _hotKey = IntPtr.Zero;
        log.LogWarning("Registering the global hotkey failed: OSStatus {Status}", status);
        return false;
    }

    public void Dispose() => Unregister();

    private void Unregister()
    {
        if (_hotKey == IntPtr.Zero) return;
        UnregisterEventHotKey(_hotKey);
        _hotKey = IntPtr.Zero;
    }

    private static int OnHotKey(IntPtr callRef, IntPtr evt, IntPtr userData)
    {
        _current?.Pressed?.Invoke(_current, EventArgs.Empty);
        return 0;
    }
}
