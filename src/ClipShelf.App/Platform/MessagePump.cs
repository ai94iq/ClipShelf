using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClipShelf.App.Platform;

// A single hidden, message-only window that receives clipboard and hotkey messages on the UI
// thread (WinUI pumps them for us). One window is enough for both listeners.
internal static class MessagePump
{
    private const string ClassName = "ClipShelf.MessageWindow";

    private delegate IntPtr WindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    // Held in a static field so the callback behind the class registration is never collected.
    private static readonly WindowProc Proc = HandleMessage;

    private static bool _classRegistered;

    public static IntPtr Handle { get; private set; }

    public static event Action? ClipboardUpdated;

    public static event Action<int>? HotkeyPressed;

    public static void EnsureCreated()
    {
        if (Handle != IntPtr.Zero) return;

        RegisterClass();
        Handle = NativeMethods.CreateWindowEx(
            0, ClassName, ClassName, 0, 0, 0, 0, 0,
            NativeMethods.MessageOnlyParent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        if (Handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Creating the message window failed.");
    }

    private static void RegisterClass()
    {
        if (_classRegistered) return;

        var windowClass = new NativeMethods.WndClassEx
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.WndClassEx>(),
            LpfnWndProc = Marshal.GetFunctionPointerForDelegate(Proc),
            LpszClassName = ClassName,
        };

        var atom = NativeMethods.RegisterClassEx(ref windowClass);
        if (atom == 0 && Marshal.GetLastWin32Error() != NativeMethods.ErrorClassAlreadyExists)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Registering the message window class failed.");

        _classRegistered = true;
    }

    private static IntPtr HandleMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case NativeMethods.MessageClipboardUpdate:
                ClipboardUpdated?.Invoke();
                break;
            case NativeMethods.MessageHotkey:
                HotkeyPressed?.Invoke((int)wParam);
                break;
        }

        return NativeMethods.DefWindowProc(window, message, wParam, lParam);
    }
}
