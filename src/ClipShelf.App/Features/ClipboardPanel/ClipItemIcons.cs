using ClipShelf.App.Resources;

namespace ClipShelf.App.Features.ClipboardPanel;

// A pinned item offers "unpin", so the button flips its glyph.
public static class ClipItemIcons
{
    public static IconKind Pin(bool isPinned) => isPinned ? IconKind.PinOff : IconKind.Pin;
}
