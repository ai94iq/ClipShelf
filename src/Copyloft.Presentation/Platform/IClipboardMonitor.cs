namespace Copyloft.Presentation.Platform;

// The clipboard a head provides: once resolved it captures every copy into the history, and it
// writes items back. IClipboardWriter is the subset the view models use.
public interface IClipboardMonitor : IClipboardWriter;
