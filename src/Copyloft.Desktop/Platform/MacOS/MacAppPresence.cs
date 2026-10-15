using System.Diagnostics;
using System.Runtime.Versioning;
using Avalonia.Threading;
using static Copyloft.Desktop.Platform.MacOS.MacNative;

namespace Copyloft.Desktop.Platform.MacOS;

// While the main window is open Copyloft is a regular app, like Notes: its menu sits next to the
// Apple menu and it has a Dock icon. Closed, it goes back to living only in the menu bar. The
// bundle must not set LSUIElement: with it, macOS keeps the app out of the menu bar whatever the
// policy says (seen on macOS 15.8.1).
[SupportedOSPlatform("macos")]
internal static class MacAppPresence
{
    private const nuint Regular = 0;       // NSApplicationActivationPolicyRegular
    private const nuint Accessory = 1;     // NSApplicationActivationPolicyAccessory
    private const nuint IgnoringOtherApps = 1 << 1;      // NSApplicationActivateIgnoringOtherApps

    public static void Set(bool regular)
    {
        var app = Send(Class("NSApplication"), "sharedApplication");
        SendBool(app, "setActivationPolicy:", regular ? Regular : Accessory);
        if (!regular) return;

        // Showing the window already activated the app while it was still menu-bar only, and macOS
        // does not hand the menu bar to an app that turns regular while active (Finder's menu stayed;
        // activating again in place did nothing). Passing focus to the Dock and back does.
        var dock = Process.GetProcessesByName("Dock").FirstOrDefault()?.Id;
        if (dock is { } pid) Activate(pid);
        DispatcherTimer.RunOnce(() => Activate(Environment.ProcessId), TimeSpan.FromMilliseconds(200));
    }

    private static void Activate(int pid)
    {
        var running = SendPid(Class("NSRunningApplication"), "runningApplicationWithProcessIdentifier:", pid);
        if (running != IntPtr.Zero) SendBool(running, "activateWithOptions:", IgnoringOtherApps);
    }
}
