using System.Runtime.InteropServices;

namespace ClipShelf.Desktop.Platform.Linux;

// Pure target negotiation and wire format for the X11 clipboard, no display
// needed. Reads prefer images over text and UTF-8 over legacy strings; served
// content advertises TARGETS plus exactly the formats the current payload
// supports. Event layout notes (Xlib/Xfixes headers, 64-bit): XEvent is a
// 192-byte union. XFixesSelectionNotifyEvent is type int32@0, serial@8,
// send_event@16, display@24, window@32, owner@40, selection@48, timestamp@56.
// SelectionNotify is type 31 with requestor@32, selection@40, target@48,
// property@56 (None means refused). SelectionRequest is type 30 with
// owner@32, requestor@40, selection@48, target@56, property@64, time@72.
public static class X11Selection
{
    public const string Targets = "TARGETS";
    public const string Utf8 = "UTF8_STRING";
    public const string Ansi = "STRING";
    public const string Text = "TEXT";
    public const string PngMime = "image/png";
    public const string Incr = "INCR";

    internal const int SelectionClear = 29;
    internal const int SelectionRequest = 30;
    internal const int SelectionNotify = 31;
    internal const int SelectionNotifyResponse = 32;

    public const int EventSize = 192;

    // What to request for a fresh copy: an image wins over any text the same
    // copy may carry, then UTF-8, then the legacy string.
    public static string? PickReadTarget(IEnumerable<string> offered)
    {
        var set = new HashSet<string>(offered, StringComparer.Ordinal);
        if (set.Contains(PngMime)) return PngMime;
        if (set.Contains(Utf8)) return Utf8;
        if (set.Contains(Ansi)) return Ansi;
        if (set.Contains(Text)) return Text;
        return null;
    }

    // What to list when another app asks what we serve.
    public static string[] ServeTargets(bool hasImage)
    {
        if (hasImage) return [Targets, PngMime, Utf8, Ansi, Text];
        return [Targets, Utf8, Ansi, Text];
    }

    // Public for headless coverage of the wire format; the pump is the only
    // production caller.
    public static int TypeOf(byte[] evt) => BitConverter.ToInt32(evt, 0);

    public static IntPtr OwnerOf(byte[] evt) => ReadPtr(evt, 40);

    public static IntPtr SelectionOf(byte[] evt) => ReadPtr(evt, 48);

    public static bool TryParseSelectionNotify(byte[] evt, IntPtr window, IntPtr selection,
        out IntPtr target, out IntPtr property)
    {
        target = IntPtr.Zero;
        property = IntPtr.Zero;
        if (TypeOf(evt) != SelectionNotify) return false;
        if (ReadPtr(evt, 32) != window) return false;
        if (ReadPtr(evt, 40) != selection) return false;
        target = ReadPtr(evt, 48);
        property = ReadPtr(evt, 56);
        return property != IntPtr.Zero;
    }

    public static bool TryParseSelectionRequest(byte[] evt,
        out IntPtr requestor, out IntPtr selection, out IntPtr target, out IntPtr property)
    {
        requestor = IntPtr.Zero;
        selection = IntPtr.Zero;
        target = IntPtr.Zero;
        property = IntPtr.Zero;
        if (TypeOf(evt) != SelectionRequest) return false;
        requestor = ReadPtr(evt, 40);
        selection = SelectionOf(evt);
        target = ReadPtr(evt, 56);
        property = ReadPtr(evt, 64);
        return true;
    }

    // Builds the SelectionNotify reply to a request. A None property reports
    // refusal for targets we do not serve.
    public static byte[] BuildSelectionResponse(
        IntPtr display, IntPtr requestor, IntPtr selection, IntPtr target, IntPtr property)
    {
        var reply = new byte[EventSize];
        BitConverter.GetBytes(SelectionNotifyResponse).CopyTo(reply, 0);
        BitConverter.GetBytes(1).CopyTo(reply, 16);
        var handle = GCHandle.Alloc(reply, GCHandleType.Pinned);
        try
        {
            var baseAddress = handle.AddrOfPinnedObject();
            Marshal.WriteIntPtr(baseAddress, 24, display);
            Marshal.WriteIntPtr(baseAddress, 32, requestor);
            Marshal.WriteIntPtr(baseAddress, 40, selection);
            Marshal.WriteIntPtr(baseAddress, 48, target);
            Marshal.WriteIntPtr(baseAddress, 56, property);
        }
        finally
        {
            handle.Free();
        }

        return reply;
    }

    private static IntPtr ReadPtr(byte[] buffer, int offset)
    {
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.ReadIntPtr(handle.AddrOfPinnedObject(), offset);
        }
        finally
        {
            handle.Free();
        }
    }
}
