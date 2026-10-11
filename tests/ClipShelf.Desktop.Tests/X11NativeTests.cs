using ClipShelf.Desktop.Platform.Linux;

namespace ClipShelf.Desktop.Tests;

// Headless coverage for the X11 wire format: fabricated event bytes in,
// parsed fields out. Runs anywhere; x64 long layout assumed.
public sealed class X11NativeTests
{
    [Fact]
    public void SelectionNotify_ParsesMatchingRead()
    {
        var window = new IntPtr(0x11);
        var selection = new IntPtr(0x22);
        var evt = Event(31, (32, window), (40, selection), (48, new IntPtr(0x33)), (56, new IntPtr(0x44)));

        Assert.True(X11Selection.TryParseSelectionNotify(evt, window, selection, out var target, out var property));
        Assert.Equal(new IntPtr(0x33), target);
        Assert.Equal(new IntPtr(0x44), property);
    }

    [Fact]
    public void SelectionNotify_RejectsRefusalsAndStrangers()
    {
        var window = new IntPtr(0x11);
        var selection = new IntPtr(0x22);
        var refused = Event(31, (32, window), (40, selection), (56, IntPtr.Zero));
        var stranger = Event(31, (32, new IntPtr(0x99)), (40, selection), (56, new IntPtr(0x44)));
        var wrongType = Event(30, (32, window), (40, selection), (56, new IntPtr(0x44)));

        Assert.False(X11Selection.TryParseSelectionNotify(refused, window, selection, out _, out _));
        Assert.False(X11Selection.TryParseSelectionNotify(stranger, window, selection, out _, out _));
        Assert.False(X11Selection.TryParseSelectionNotify(wrongType, window, selection, out _, out _));
    }

    [Fact]
    public void SelectionRequest_ParsesFields()
    {
        var evt = Event(30, (40, new IntPtr(0x51)), (48, new IntPtr(0x52)), (56, new IntPtr(0x53)), (64, new IntPtr(0x54)));

        Assert.True(X11Selection.TryParseSelectionRequest(evt, out var requestor, out var selection, out var target, out var property));
        Assert.Equal(new IntPtr(0x51), requestor);
        Assert.Equal(new IntPtr(0x52), selection);
        Assert.Equal(new IntPtr(0x53), target);
        Assert.Equal(new IntPtr(0x54), property);
    }

    [Fact]
    public void SelectionResponse_RoundTripsFields()
    {
        var reply = X11Selection.BuildSelectionResponse(
            new IntPtr(0x01), new IntPtr(0x02), new IntPtr(0x03), new IntPtr(0x04), new IntPtr(0x05));

        Assert.Equal(32, BitConverter.ToInt32(reply, 0));
        Assert.Equal(0x02, BitConverter.ToInt64(reply, 32));
        Assert.Equal(0x03, BitConverter.ToInt64(reply, 40));
        Assert.Equal(0x04, BitConverter.ToInt64(reply, 48));
        Assert.Equal(0x05, BitConverter.ToInt64(reply, 56));
    }

    private static byte[] Event(int type, params (int Offset, IntPtr Value)[] fields)
    {
        var bytes = new byte[X11Selection.EventSize];
        BitConverter.GetBytes(type).CopyTo(bytes, 0);
        foreach (var (offset, value) in fields)
            BitConverter.GetBytes(value.ToInt64()).CopyTo(bytes, offset);
        return bytes;
    }
}
