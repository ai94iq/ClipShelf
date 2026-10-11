using Avalonia.Controls;

namespace ClipShelf.Desktop.Features;

// What the shared flyout actions need from their host window: hiding it and,
// where the platform tracks it, the previously focused window for paste-back.
public interface IFlyoutHost
{
    void HideFlyout();

    IntPtr PreviousWindowHandle { get; }

    Control Anchor { get; }
}
