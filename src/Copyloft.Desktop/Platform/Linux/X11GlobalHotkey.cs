using System.Runtime.Versioning;
using Copyloft.Core.Input;
using static Copyloft.Desktop.Platform.Linux.X11Native;

namespace Copyloft.Desktop.Platform.Linux;

// Grabs one key combination on the root window (plus its Caps Lock and Num Lock variants) and
// raises Pressed on the UI thread, once per press: a held key's repeats are ignored.
[SupportedOSPlatform("linux")]
public sealed class X11GlobalHotkey : IGlobalHotkey
{
    private readonly X11Connection _x;
    private readonly IUiDispatcher _ui;
    private readonly ILogger<X11GlobalHotkey> _log;
    private (int Keycode, uint Mask)? _grab;
    private bool _down;

    public X11GlobalHotkey(X11Connection x, IUiDispatcher ui, ILogger<X11GlobalHotkey> log)
    {
        _x = x;
        _ui = ui;
        _log = log;
        _x.KeyChanged += OnKeyChanged;
    }

    public event EventHandler? Pressed;

    public bool Register(HotkeyGesture gesture)
    {
        var registered = _x.Run(() =>
        {
            Ungrab();
            var keycode = XKeysymToKeycode(_x.Display, (IntPtr)X11Keys.Keysym(gesture.VirtualKey));
            if (keycode == 0) return false;

            var mask = X11Keys.Mask(gesture.Modifiers);
            var grab = (Keycode: (int)keycode, Mask: mask);
            if (!_x.Trap(() => Grab(grab, on: true)))
            {
                _x.Trap(() => Grab(grab, on: false));
                return false;
            }
            _grab = grab;
            return true;
        }).GetAwaiter().GetResult();

        if (!registered) _log.LogWarning("Grabbing the global hotkey failed; another app may own it");
        return registered;
    }

    public void Dispose()
    {
        _x.KeyChanged -= OnKeyChanged;
        _x.Run(() => { Ungrab(); return true; });
    }

    private void Grab((int Keycode, uint Mask) grab, bool on)
    {
        foreach (var mask in X11Keys.GrabMasks(grab.Mask))
        {
            if (on) XGrabKey(_x.Display, grab.Keycode, mask, _x.Root, 0, GrabModeAsync, GrabModeAsync);
            else XUngrabKey(_x.Display, grab.Keycode, mask, _x.Root);
        }
    }

    private void Ungrab()
    {
        if (_grab is { } grab) _x.Trap(() => Grab(grab, on: false));
        _grab = null;
    }

    // On the X thread.
    internal void OnKeyChanged(uint keycode, uint state, bool pressed)
    {
        if (_grab is not { } grab || keycode != grab.Keycode) return;
        if (!pressed)
        {
            _down = false;
            return;
        }
        if (_down || !X11Keys.Matches(state, grab.Mask)) return;
        _down = true;
        _ui.Post(() => Pressed?.Invoke(this, EventArgs.Empty));
    }
}
