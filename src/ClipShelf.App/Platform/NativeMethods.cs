using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054 // Plain DllImport is enough here; LibraryImport adds no value for these.

namespace ClipShelf.App.Platform;

// The Win32 surface used for clipboard access, the change listener and the global hotkey.
internal static class NativeMethods
{
    internal const uint MessageClipboardUpdate = 0x031D;
    internal const uint MessageHotkey = 0x0312;

    internal const uint ClipboardFormatUnicodeText = 13;
    internal const uint GlobalMemoryMoveable = 0x0002;
    internal const uint GlobalMemoryZeroInit = 0x0040;

    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    internal const uint ModWin = 0x0008;

    internal const int ErrorClassAlreadyExists = 1410;

    // HWND_MESSAGE (-3): a message-only window that is never shown.
    internal static readonly IntPtr MessageOnlyParent = new(-3);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool OpenClipboard(IntPtr owner);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetClipboardData(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalFree(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool AddClipboardFormatListener(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RemoveClipboardFormatListener(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr window, int id);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern ushort RegisterClassEx(ref WndClassEx windowClass);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CreateWindowEx(
        uint extendedStyle, string className, string windowName, uint style,
        int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WndClassEx
    {
        internal uint CbSize;
        internal uint Style;
        internal IntPtr LpfnWndProc;
        internal int CbClsExtra;
        internal int CbWndExtra;
        internal IntPtr HInstance;
        internal IntPtr HIcon;
        internal IntPtr HCursor;
        internal IntPtr HbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] internal string? LpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] internal string LpszClassName;
        internal IntPtr HIconSm;
    }
}
