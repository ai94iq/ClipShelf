using System.Runtime.Versioning;
using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform.Linux;

// Global hotkey until XGrabKey plus the GlobalShortcuts portal land (Phase 3
// task): registration reports failure so the settings page shows the hotkey
// warning instead of pretending the shortcut works.
[SupportedOSPlatform("linux")]
public sealed class LinuxHotkey : IGlobalHotkey
{
#pragma warning disable CS0067 // Raised by the XGrabKey implementation in a later Phase 3 task.
    public event EventHandler? Pressed;
#pragma warning restore CS0067

    public bool Register(HotkeyGesture gesture) => false;

    public void Dispose()
    {
    }
}
