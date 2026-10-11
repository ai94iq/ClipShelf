using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Linux clipboard surface until the X11/Wayland watch lands (Phase 3 task):
// no change events, no reads, writes are dropped. The app boots and the
// history window works; capture and paste-back stay unavailable.
[SupportedOSPlatform("linux")]
public sealed class LinuxClipboard : IClipboardMonitor
{
#pragma warning disable CS0067 // Raised by the X11/Wayland watch in a later Phase 3 task.
    public event EventHandler? Changed;
#pragma warning restore CS0067

    public string? ReadText() => null;

    public byte[]? ReadImage() => null;

    public void WriteText(string text)
    {
    }

    public Task WriteImageAsync(byte[] png) => Task.CompletedTask;
}
