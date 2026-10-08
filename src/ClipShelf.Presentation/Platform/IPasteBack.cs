namespace ClipShelf.Presentation.Platform;

// Sends a paste keystroke to the window the user was in before the flyout opened.
public interface IPasteBack
{
    Task IntoAsync(IntPtr targetWindow);
}
