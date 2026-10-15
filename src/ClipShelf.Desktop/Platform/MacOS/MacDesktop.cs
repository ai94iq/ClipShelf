using System.Runtime.Versioning;
using Avalonia;
using static ClipShelf.Desktop.Platform.MacOS.MacNative;

namespace ClipShelf.Desktop.Platform.MacOS;

// The frontmost app (its process id stands in for "the window" the flyout returns to), the
// pointer, activation, and Cmd+V through a CoreGraphics event. Posting keys needs the
// Accessibility permission; without it the pick is only copied, and macOS is asked once.
[SupportedOSPlatform("macos")]
public sealed class MacDesktop : IForegroundApp, IFlyoutPlatform, IPasteBack
{
    private const int DelayMs = 60;
    private const ushort KeyV = 0x09;                    // kVK_ANSI_V
    private const ulong CommandFlag = 0x100000;          // kCGEventFlagMaskCommand
    private const nuint IgnoringOtherApps = 1 << 1;      // NSApplicationActivateIgnoringOtherApps
    private bool _askedForAccess;

    public string? Name() => String(Send(Frontmost(), "localizedName"));

    public IntPtr ForegroundWindow()
    {
        var app = Frontmost();
        return app == IntPtr.Zero ? IntPtr.Zero : SendInt(app, "processIdentifier");
    }

    // CoreGraphics reports points from the top-left of the main display; Avalonia places in pixels.
    public PixelPoint Cursor()
    {
        var evt = CGEventCreate(IntPtr.Zero);
        var point = CGEventGetLocation(evt);
        CFRelease(evt);
        var scale = SendDouble(Send(Class("NSScreen"), "mainScreen"), "backingScaleFactor");
        return new PixelPoint((int)(point.X * scale), (int)(point.Y * scale));
    }

    public void ForceForeground(IntPtr window) => Activate(Environment.ProcessId);

    public void RestoreForeground(IntPtr own, IntPtr previous)
    {
        if (previous != IntPtr.Zero && ForegroundWindow() == Environment.ProcessId) Activate((int)previous);
    }

    public bool CanPaste
    {
        get
        {
            if (AXIsProcessTrusted()) return true;
            if (!_askedForAccess)
            {
                _askedForAccess = true;
                AskForAccess();
            }
            return false;
        }
    }

    public async Task IntoAsync(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero) return;
        Activate((int)targetWindow);
        await Task.Delay(DelayMs);
        foreach (var down in new[] { true, false })
        {
            var evt = CGEventCreateKeyboardEvent(IntPtr.Zero, KeyV, down);
            CGEventSetFlags(evt, CommandFlag);
            CGEventPost(0, evt);                          // kCGHIDEventTap
            CFRelease(evt);
        }
    }

    private static IntPtr Frontmost() => Send(Send(Class("NSWorkspace"), "sharedWorkspace"), "frontmostApplication");

    private static void Activate(int pid)
    {
        var app = SendPid(Class("NSRunningApplication"), "runningApplicationWithProcessIdentifier:", pid);
        if (app != IntPtr.Zero) SendBool(app, "activateWithOptions:", IgnoringOtherApps);
    }

    // AXIsProcessTrustedWithOptions({kAXTrustedCheckOptionPrompt: true}) shows the system prompt
    // that leads to Privacy & Security > Accessibility.
    private static void AskForAccess()
    {
        var keys = new[] { Constant(ApplicationServicesPath, "kAXTrustedCheckOptionPrompt") };
        var values = new[] { Constant(CoreFoundationPath, "kCFBooleanTrue") };
        var options = CFDictionaryCreate(
            IntPtr.Zero, keys, values, 1,
            Export(CoreFoundationPath, "kCFTypeDictionaryKeyCallBacks"),
            Export(CoreFoundationPath, "kCFTypeDictionaryValueCallBacks"));
        AXIsProcessTrustedWithOptions(options);
        CFRelease(options);
    }
}
