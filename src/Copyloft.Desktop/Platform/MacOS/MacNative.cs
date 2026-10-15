using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#pragma warning disable SYSLIB1054 // DllImport is intentional: the app ships self-contained, not Native AOT.

namespace Copyloft.Desktop.Platform.MacOS;

// The Objective-C runtime, CoreGraphics, Accessibility, Carbon hotkey and Keychain calls the macOS
// services use. objc_msgSend is bound once per signature it is called with (required on arm64).
[SupportedOSPlatform("macos")]
internal static class MacNative
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";
    private const string Security = "/System/Library/Frameworks/Security.framework/Security";

    internal const int ErrSecItemNotFound = -25300;

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGPoint
    {
        public double X;
        public double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventTypeSpec
    {
        public uint EventClass;
        public uint EventKind;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventHotKeyId
    {
        public uint Signature;
        public uint Id;
    }

    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";

    // [Class selector], [object selector] and friends. objc_getClass finds AppKit classes
    // (NSPasteboard, NSWorkspace) only once AppKit is loaded; the app gets it from Avalonia's native
    // backend, so it is loaded here too, or callers that run first (tests) get nil.
    internal static IntPtr Class(string name)
    {
        _ = AppKitLoaded.Value;
        return objc_getClass(name);
    }

    private static readonly Lazy<bool> AppKitLoaded = new(() => NativeLibrary.TryLoad(AppKit, out _));

    internal static IntPtr Selector(string name) => sel_registerName(name);

    internal static IntPtr Send(IntPtr receiver, string selector) => objc_msgSend(receiver, Selector(selector));

    internal static IntPtr Send(IntPtr receiver, string selector, IntPtr argument) =>
        objc_msgSend(receiver, Selector(selector), argument);

    internal static nint SendInteger(IntPtr receiver, string selector) => objc_msgSend_nint(receiver, Selector(selector));

    internal static double SendDouble(IntPtr receiver, string selector) => objc_msgSend_double(receiver, Selector(selector));

    internal static int SendInt(IntPtr receiver, string selector) => objc_msgSend_int(receiver, Selector(selector));

    internal static bool SendBool(IntPtr receiver, string selector, nuint argument) =>
        objc_msgSend_bool_nuint(receiver, Selector(selector), argument);

    internal static IntPtr SendPid(IntPtr receiver, string selector, int pid) =>
        objc_msgSend_int_arg(receiver, Selector(selector), pid);

    // An NSString as a .NET string (null for nil).
    internal static string? String(IntPtr nsString) =>
        nsString == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(Send(nsString, "UTF8String"));

    [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
    [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
    [DllImport(ObjC)] private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr argument);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern nint objc_msgSend_nint(IntPtr receiver, IntPtr selector);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern double objc_msgSend_double(IntPtr receiver, IntPtr selector);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern int objc_msgSend_int(IntPtr receiver, IntPtr selector);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr objc_msgSend_int_arg(IntPtr receiver, IntPtr selector, int argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_nuint(IntPtr receiver, IntPtr selector, nuint argument);

    [DllImport(CoreFoundation)] internal static extern void CFRelease(IntPtr value);

    [DllImport(CoreFoundation)]
    internal static extern IntPtr CFDictionaryCreate(
        IntPtr allocator, IntPtr[] keys, IntPtr[] values, nint count, IntPtr keyCallBacks, IntPtr valueCallBacks);

    [DllImport(CoreGraphics)] internal static extern IntPtr CGEventCreate(IntPtr source);
    [DllImport(CoreGraphics)] internal static extern CGPoint CGEventGetLocation(IntPtr evt);
    [DllImport(CoreGraphics)] internal static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort keyCode, [MarshalAs(UnmanagedType.I1)] bool keyDown);
    [DllImport(CoreGraphics)] internal static extern void CGEventSetFlags(IntPtr evt, ulong flags);
    [DllImport(CoreGraphics)] internal static extern void CGEventPost(uint tap, IntPtr evt);
    [DllImport(CoreGraphics)] internal static extern uint CGMainDisplayID();
    [DllImport(CoreGraphics)] internal static extern int CGDisplayIsAsleep(uint display);   // boolean_t

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXIsProcessTrusted();

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

    // The value of an exported constant (a CFStringRef or CFBooleanRef global).
    internal static IntPtr Constant(string library, string symbol) =>
        Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(library), symbol));

    internal static IntPtr Export(string library, string symbol) =>
        NativeLibrary.GetExport(NativeLibrary.Load(library), symbol);

    internal const string CoreFoundationPath = CoreFoundation;
    internal const string ApplicationServicesPath = ApplicationServices;

    [DllImport(Carbon)] internal static extern IntPtr GetApplicationEventTarget();

    [DllImport(Carbon)]
    internal static extern int InstallEventHandler(
        IntPtr target, IntPtr handler, uint typeCount, ref EventTypeSpec types, IntPtr userData, out IntPtr handlerRef);

    [DllImport(Carbon)]
    internal static extern int RegisterEventHotKey(
        uint keyCode, uint modifiers, EventHotKeyId id, IntPtr target, uint options, out IntPtr hotKeyRef);

    [DllImport(Carbon)] internal static extern int UnregisterEventHotKey(IntPtr hotKeyRef);

    [DllImport(Security)]
    internal static extern int SecKeychainFindGenericPassword(
        IntPtr keychainOrArray, uint serviceLength, byte[] service, uint accountLength, byte[] account,
        out uint passwordLength, out IntPtr passwordData, IntPtr itemRef);

    [DllImport(Security)]
    internal static extern int SecKeychainAddGenericPassword(
        IntPtr keychain, uint serviceLength, byte[] service, uint accountLength, byte[] account,
        uint passwordLength, byte[] password, IntPtr itemRef);

    [DllImport(Security)] internal static extern int SecKeychainItemFreeContent(IntPtr attributes, IntPtr data);
}
