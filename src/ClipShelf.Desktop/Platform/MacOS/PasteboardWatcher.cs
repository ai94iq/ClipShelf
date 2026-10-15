using System.Runtime.Versioning;
using Avalonia.Threading;
using static ClipShelf.Desktop.Platform.MacOS.MacNative;

namespace ClipShelf.Desktop.Platform.MacOS;

// macOS has no clipboard-change notification; the general pasteboard's changeCount is polled on
// the UI thread instead, and Changed fires when it moves.
[SupportedOSPlatform("macos")]
public sealed class PasteboardWatcher : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(300);
    private readonly DispatcherTimer _timer;
    private readonly IntPtr _pasteboard = Send(Class("NSPasteboard"), "generalPasteboard");
    private nint _count;

    public PasteboardWatcher()
    {
        _count = ChangeCount();
        _timer = new DispatcherTimer(Interval, DispatcherPriority.Background, (_, _) => Poll());
        _timer.Start();
    }

    public event Action? Changed;

    public nint ChangeCount() => SendInteger(_pasteboard, "changeCount");

    public void Dispose() => _timer.Stop();

    private void Poll()
    {
        var count = ChangeCount();
        if (count == _count) return;
        _count = count;
        Changed?.Invoke();
    }
}
