using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClipShelf.App.Platform;

// A single hidden, top-level window that receives clipboard, hotkey and system setting messages
// on the UI thread (WinUI pumps them for us). One window is enough for all listeners. It is
// top-level rather than message-only because Windows broadcasts theme changes (WM_SETTINGCHANGE
// with "ImmersiveColorSet") only to top-level windows.
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

    // Raised when Windows switches the light/dark theme; the notification area follows the
    // taskbar variant, which is read from the registry when this fires.
    public static event Action? SystemThemeChanged;

    public static void EnsureCreated()
    {
        if (Handle != IntPtr.Zero) return;

        RegisterClass();
        Handle = NativeMethods.CreateWindowEx(
            0, ClassName, ClassName, 0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

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
            case NativeMethods.MessageSettingChange:
                if (IsImmersiveColorSet(lParam)) SystemThemeChanged?.Invoke();
                break;
            case NativeMethods.MessageThemeChanged:
                SystemThemeChanged?.Invoke();
                break;
        }

        return NativeMethods.DefWindowProc(window, message, wParam, lParam);
    }

    private static bool IsImmersiveColorSet(IntPtr lParam) =>
        lParam != IntPtr.Zero
        && string.Equals(Marshal.PtrToStringUni(lParam), "ImmersiveColorSet", StringComparison.OrdinalIgnoreCase);
}
