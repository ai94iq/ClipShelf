using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ClipShelf.Desktop.Platform.Linux;

// Minimal Xlib/Xfixes surface for clipboard watch, reads and serving. Every
// native call must happen on the pump thread; Xlib is not thread-safe. Pure
// wire-format parsing lives on X11Selection next door.
[SupportedOSPlatform("linux")]
internal static class X11Native
{
    internal const int XFixesSelectionOwnerNotify = 0;
    internal const ulong XFixesSelectionOwnerNotifyMask = 1;

    internal const long AllBytes = 0x1FFFFFFFL;
    internal const ulong AnyPropertyType = 0;
    internal const int PropModeReplace = 0;
    internal static readonly IntPtr CurrentTime = IntPtr.Zero;
    internal static readonly IntPtr None = IntPtr.Zero;

    [DllImport("libX11.so.6", CharSet = CharSet.Ansi)]
    internal static extern IntPtr XOpenDisplay(string? display);

    [DllImport("libX11.so.6")]
    internal static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    internal static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    internal static extern IntPtr XCreateSimpleWindow(
        IntPtr display, IntPtr parent, int x, int y, uint width, uint height,
        uint borderWidth, ulong border, ulong background);

    [DllImport("libX11.so.6", CharSet = CharSet.Ansi)]
    internal static extern IntPtr XInternAtom(IntPtr display, string name, bool onlyIfExists);

    [DllImport("libX11.so.6", CharSet = CharSet.Ansi)]
    internal static extern IntPtr XGetAtomName(IntPtr display, IntPtr atom);

    [DllImport("libXfixes.so.3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool XFixesQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [DllImport("libXfixes.so.3")]
    internal static extern void XFixesSelectSelectionInput(
        IntPtr display, IntPtr window, IntPtr selection, ulong eventMask);

    [DllImport("libX11.so.6")]
    internal static extern int XPending(IntPtr display);

    [DllImport("libX11.so.6")]
    internal static extern int XNextEvent(IntPtr display, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst = X11Selection.EventSize)] byte[] evt);

    [DllImport("libX11.so.6")]
    internal static extern int XConvertSelection(
        IntPtr display, IntPtr selection, IntPtr target, IntPtr property,
        IntPtr requestor, IntPtr time);

    [DllImport("libX11.so.6")]
    internal static extern int XGetWindowProperty(
        IntPtr display, IntPtr window, IntPtr property, IntPtr longOffset, IntPtr longLength,
        bool delete, IntPtr reqType, out IntPtr actualType, out int actualFormat,
        out ulong itemCount, out ulong bytesAfter, out IntPtr value);

    [DllImport("libX11.so.6")]
    internal static extern int XChangeProperty(
        IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format,
        int mode, byte[] data, int elementCount);

    [DllImport("libX11.so.6")]
    internal static extern int XDeleteProperty(IntPtr display, IntPtr window, IntPtr property);

    [DllImport("libX11.so.6")]
    internal static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

    [DllImport("libX11.so.6")]
    internal static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

    [DllImport("libX11.so.6")]
    internal static extern int XSendEvent(
        IntPtr display, IntPtr window, bool propagate, IntPtr eventMask,
        [MarshalAs(UnmanagedType.LPArray, SizeConst = X11Selection.EventSize)] byte[] evt);

    [DllImport("libX11.so.6")]
    internal static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    internal static extern int XFree(IntPtr data);

    [DllImport("libX11.so.6")]
    internal static extern int XQueryPointer(
        IntPtr display, IntPtr window, out IntPtr root, out IntPtr child,
        out int rootX, out int rootY, out int windowX, out int windowY, out uint mask);

    [DllImport("libX11.so.6")]
    internal static extern uint XKeysymToKeycode(IntPtr display, ulong keysym);

    [DllImport("libX11.so.6", CharSet = CharSet.Ansi)]
    internal static extern ulong XStringToKeysym(string name);

    [DllImport("libX11.so.6")]
    internal static extern int XGrabKey(
        IntPtr display, int keycode, uint modifiers, IntPtr grabWindow,
        bool ownerEvents, int pointerMode, int keyboardMode);

    [DllImport("libX11.so.6")]
    internal static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

    [DllImport("libX11.so.6")]
    internal static extern int XSync(IntPtr display, bool discard);

    internal delegate int XErrorHandler(ref XErrorEvent error);

    internal static IntPtr XSetErrorHandler(XErrorHandler handler) =>
        SetErrorHandlerByDelegate(handler);

    internal static IntPtr XSetErrorHandler(IntPtr previous) =>
        SetErrorHandlerByPointer(previous);

    [DllImport("libX11.so.6", EntryPoint = "XSetErrorHandler")]
    private static extern IntPtr SetErrorHandlerByDelegate(XErrorHandler handler);

    [DllImport("libX11.so.6", EntryPoint = "XSetErrorHandler")]
    private static extern IntPtr SetErrorHandlerByPointer(IntPtr previous);

    [StructLayout(LayoutKind.Sequential)]
    internal struct XErrorEvent
    {
        internal int Type;
        internal IntPtr Display;
        internal IntPtr ResourceId;
        internal IntPtr Serial;
        internal byte ErrorCode;
        internal byte RequestCode;
        internal byte MinorCode;
    }

    internal static string ReadAtomName(IntPtr display, IntPtr atom)
    {
        var name = XGetAtomName(display, atom);
        if (name == IntPtr.Zero) return string.Empty;
        try
        {
            return Marshal.PtrToStringUTF8(name) ?? string.Empty;
        }
        finally
        {
            XFree(name);
        }
    }

    internal static byte[]? ReadBytes(IntPtr display, IntPtr window, IntPtr property, IntPtr incrAtom, bool remove = true)
    {
        var status = XGetWindowProperty(display, window, property, IntPtr.Zero, (IntPtr)AllBytes,
            remove, (IntPtr)AnyPropertyType, out var type, out var format, out var count, out _,
            out var value);
        if (status != 0 || value == IntPtr.Zero) return null;
        try
        {
            // INCR (staged large transfers) is a later task; format 32 arrives as
            // longs, so the byte count is eight per item.
            if (type == incrAtom) return null;
            if (format != 8 && format != 32) return null;
            var byteCount = format == 32 ? checked((int)count * 8) : (int)count;
            if (byteCount == 0) return null;
            var bytes = new byte[byteCount];
            Marshal.Copy(value, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            XFree(value);
        }
    }
}
