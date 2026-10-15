using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#pragma warning disable SYSLIB1054 // DllImport is intentional: the app ships self-contained, not Native AOT.

namespace ClipShelf.Desktop.Platform.Linux;

// The Xlib, XFixes and XTest surface used for the clipboard watch, the hotkey grab, paste-back,
// the active window and the pointer. Bool is the 4-byte C int Xlib uses; X ids are C longs.
[SupportedOSPlatform("linux")]
internal static class X11Native
{
    private const string X11 = "libX11.so.6";
    private const string Xfixes = "libXfixes.so.3";
    private const string Xtst = "libXtst.so.6";

    internal const int KeyPress = 2;
    internal const int KeyRelease = 3;
    internal const int ClientMessage = 33;
    internal const int GrabModeAsync = 1;
    internal const int BadAccess = 10;
    internal const nuint XFixesSetSelectionOwnerNotifyMask = 1;
    internal const long SubstructureNotifyMask = 1L << 19;
    internal const long SubstructureRedirectMask = 1L << 20;

    // XEvent is a union padded to 24 longs; only the fields read here are named.
    [StructLayout(LayoutKind.Explicit, Size = 192)]
    internal struct XEvent
    {
        [FieldOffset(0)] public int Type;
        [FieldOffset(80)] public uint KeyState;
        [FieldOffset(84)] public uint KeyCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XClientMessageEvent
    {
        public int Type;
        public nuint Serial;
        public int SendEvent;
        public IntPtr Display;
        public IntPtr Window;
        public IntPtr MessageType;
        public int Format;
        public IntPtr Data0, Data1, Data2, Data3, Data4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XErrorEvent
    {
        public int Type;
        public IntPtr Display;
        public IntPtr ResourceId;
        public nuint Serial;
        public byte ErrorCode;
        public byte RequestCode;
        public byte MinorCode;
    }

    [DllImport(X11)] internal static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport(X11)] internal static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport(X11)] internal static extern IntPtr XInternAtom(IntPtr display, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int onlyIfExists);
    [DllImport(X11)] internal static extern int XNextEvent(IntPtr display, out XEvent e);
    [DllImport(X11)] internal static extern int XFlush(IntPtr display);
    [DllImport(X11)] internal static extern int XSync(IntPtr display, int discard);
    [DllImport(X11)] internal static extern int XFree(IntPtr data);
    [DllImport(X11)] internal static extern byte XKeysymToKeycode(IntPtr display, IntPtr keysym);
    [DllImport(X11)] internal static extern int XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr window, int ownerEvents, int pointerMode, int keyboardMode);
    [DllImport(X11)] internal static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr window);
    [DllImport(X11)] internal static extern IntPtr XSetErrorHandler(IntPtr handler);
    [DllImport(X11)] internal static extern int XkbSetDetectableAutoRepeat(IntPtr display, int detectable, out int supported);
    [DllImport(X11)] internal static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint borderWidth, nuint border, nuint background);
    [DllImport(X11)] internal static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

    [DllImport(X11)]
    internal static extern int XQueryPointer(
        IntPtr display, IntPtr window, out IntPtr root, out IntPtr child,
        out int rootX, out int rootY, out int winX, out int winY, out uint mask);

    [DllImport(X11)]
    internal static extern int XGetWindowProperty(
        IntPtr display, IntPtr window, IntPtr property, nint offset, nint length, int delete, IntPtr requestedType,
        out IntPtr actualType, out int actualFormat, out nuint items, out nuint bytesAfter, out IntPtr data);

    [DllImport(X11)]
    internal static extern int XSendEvent(IntPtr display, IntPtr window, int propagate, nint mask, ref XClientMessageEvent e);

    [DllImport(Xfixes)] internal static extern int XFixesQueryExtension(IntPtr display, out int eventBase, out int errorBase);
    [DllImport(Xfixes)] internal static extern void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr selection, nuint mask);

    [DllImport(Xtst)] internal static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, int isPress, nuint delay);
}
