using ClipShelf.Core.Input;

namespace ClipShelf.Presentation.Platform;

// Registers one global shortcut and raises Pressed when it fires.
public interface IGlobalHotkey : IDisposable
{
    event EventHandler? Pressed;

    bool Register(HotkeyGesture gesture);
}
