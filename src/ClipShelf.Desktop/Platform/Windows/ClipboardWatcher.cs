using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Windows;

// Raises an event on the UI thread whenever any application changes the clipboard.
[SupportedOSPlatform("windows")]
internal sealed class ClipboardWatcher : IDisposable
{
    public ClipboardWatcher()
    {
        MessagePump.EnsureCreated();
        MessagePump.ClipboardUpdated += OnClipboardUpdated;
        NativeMethods.AddClipboardFormatListener(MessagePump.Handle);
    }

    public event EventHandler? Changed;

    public void Dispose()
    {
        NativeMethods.RemoveClipboardFormatListener(MessagePump.Handle);
        MessagePump.ClipboardUpdated -= OnClipboardUpdated;
    }

    private void OnClipboardUpdated() => Changed?.Invoke(this, EventArgs.Empty);
}
