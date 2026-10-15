using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform.Linux;
using ClipShelf.Presentation.Platform;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable SYSLIB1054

namespace ClipShelf.Desktop.Tests;

// Against the real X server (WSLg's XWayland in development). A second client plays the other
// apps: it takes the clipboard, grabs keys and types through XTest. XTest input stays inside
// the X server, and the combination used (Ctrl+Alt+Shift+F12) is bound by nothing.
[SupportedOSPlatform("linux")]
public sealed class X11LiveTests
{
    private static readonly HotkeyGesture Unused =
        new(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x7B); // F12

    private static void SkipWithoutX() =>
        Assert.SkipUnless(OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("DISPLAY") is { Length: > 0 }, "an X display");

    [Fact]
    public async Task Another_app_taking_the_clipboard_raises_selection_changed()
    {
        SkipWithoutX();
        var x = new X11Connection();
        var changed = new TaskCompletionSource();
        x.SelectionChanged += () => changed.TrySetResult();

        var other = XOpenDisplay(IntPtr.Zero);
        var window = XCreateSimpleWindow(other, XDefaultRootWindow(other), 0, 0, 1, 1, 0, 0, 0);
        XSetSelectionOwner(other, XInternAtom(other, "CLIPBOARD", 0), window, IntPtr.Zero);
        XFlush(other);

        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        XCloseDisplay(other);
    }

    [Fact]
    public async Task A_grabbed_combination_fires_once_per_press()
    {
        SkipWithoutX();
        // XWayland (WSLg too) passes keys to X clients only while an X window has focus, the same
        // limit ADR 0007 records for Wayland; a plain X server (Xvfb in CI) delivers them always.
        Assert.SkipWhen(ClipShelf.Desktop.Platform.DesktopSession.IsWayland, "XWayland routes keys only to a focused X window");
        using var hotkey = new X11GlobalHotkey(new X11Connection(), new InlineUi(), NullLogger<X11GlobalHotkey>.Instance);
        var presses = 0;
        hotkey.Pressed += (_, _) => Interlocked.Increment(ref presses);
        Assert.True(hotkey.Register(Unused));

        var other = XOpenDisplay(IntPtr.Zero);
        uint[] keys = [0xFFE3, 0xFFE9, 0xFFE1, 0xFFC9]; // Control_L, Alt_L, Shift_L, F12
        foreach (var key in keys) XTestFakeKeyEvent(other, XKeysymToKeycode(other, (IntPtr)key), 1, 0);
        foreach (var key in keys.Reverse()) XTestFakeKeyEvent(other, XKeysymToKeycode(other, (IntPtr)key), 0, 0);
        XFlush(other);
        XCloseDisplay(other);

        for (var waited = 0; Volatile.Read(ref presses) == 0 && waited < 5000; waited += 50)
            await Task.Delay(50, TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken); // room for a wrong second one
        Assert.Equal(1, presses);
    }

    // On X11 the active window's process names the copy. Under Wayland the active X window is not
    // where a copy came from (in WSLg it was ClipShelf's own History window), so the name stays
    // blank. The test plays the window manager: _NET_ACTIVE_WINDOW points at a window of this process.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_source_app_is_the_active_windows_process_except_under_wayland(bool wayland)
    {
        SkipWithoutX();
        var other = XOpenDisplay(IntPtr.Zero);
        var root = XDefaultRootWindow(other);
        var window = XCreateSimpleWindow(other, root, 0, 0, 1, 1, 0, 0, 0);
        nint pid = Environment.ProcessId;
        XChangeProperty(other, window, XInternAtom(other, "_NET_WM_PID", 0), XInternAtom(other, "CARDINAL", 0), 32, 0, ref pid, 1);
        nint active = window;
        XChangeProperty(other, root, XInternAtom(other, "_NET_ACTIVE_WINDOW", 0), XInternAtom(other, "WINDOW", 0), 32, 0, ref active, 1);
        XSync(other, 0);

        var name = new X11Desktop(new X11Connection(), wayland).Name();
        XCloseDisplay(other);

        Assert.Equal(wayland ? null : System.Diagnostics.Process.GetCurrentProcess().ProcessName, name);
    }

    // A held key repeats presses with no release in between (detectable auto-repeat); XTest cannot
    // produce that, so the repeats are fed straight to the handler.
    [Fact]
    public void A_held_combination_fires_once_until_it_is_released()
    {
        SkipWithoutX();
        using var hotkey = new X11GlobalHotkey(new X11Connection(), new InlineUi(), NullLogger<X11GlobalHotkey>.Instance);
        var presses = 0;
        hotkey.Pressed += (_, _) => presses++;
        Assert.True(hotkey.Register(Unused));
        var other = XOpenDisplay(IntPtr.Zero);
        var f12 = XKeysymToKeycode(other, (IntPtr)0xFFC9);
        XCloseDisplay(other);
        var mask = X11Keys.Mask(Unused.Modifiers);

        hotkey.OnKeyChanged(f12, mask, pressed: true);
        hotkey.OnKeyChanged(f12, mask, pressed: true);
        hotkey.OnKeyChanged(f12, mask, pressed: true);
        Assert.Equal(1, presses);

        hotkey.OnKeyChanged(f12, mask, pressed: false);
        hotkey.OnKeyChanged(f12, mask, pressed: true);
        Assert.Equal(2, presses);
    }

    [Fact]
    public void A_combination_another_app_holds_is_refused()
    {
        SkipWithoutX();
        var other = XOpenDisplay(IntPtr.Zero);
        var keycode = XKeysymToKeycode(other, (IntPtr)0xFFC9);
        Assert.NotEqual(0, keycode);
        XGrabKey(other, keycode, X11Keys.Mask(Unused.Modifiers), XDefaultRootWindow(other), 0, 1, 1);
        XSync(other, 0);

        using var hotkey = new X11GlobalHotkey(new X11Connection(), new InlineUi(), NullLogger<X11GlobalHotkey>.Instance);
        var registered = hotkey.Register(Unused);
        XCloseDisplay(other);

        Assert.False(registered);
    }

    private sealed class InlineUi : IUiDispatcher
    {
        public void Post(Action action) => action();
    }

    private const string X11 = "libX11.so.6";
    [DllImport(X11)] private static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport(X11)] private static extern int XCloseDisplay(IntPtr display);
    [DllImport(X11)] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport(X11)] private static extern IntPtr XInternAtom(IntPtr display, string name, int onlyIfExists);
    [DllImport(X11)] private static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint border, nuint borderColor, nuint background);
    [DllImport(X11)] private static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);
    [DllImport(X11)] private static extern int XFlush(IntPtr display);
    [DllImport(X11)] private static extern int XSync(IntPtr display, int discard);
    [DllImport(X11)] private static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, ref nint data, int count);
    [DllImport(X11)] private static extern byte XKeysymToKeycode(IntPtr display, IntPtr keysym);
    [DllImport(X11)] private static extern int XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr window, int ownerEvents, int pointerMode, int keyboardMode);
    [DllImport("libXtst.so.6")] private static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, int isPress, nuint delay);
}
