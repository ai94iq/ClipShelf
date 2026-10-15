namespace ClipShelf.Presentation.Platform;

// Sends a paste keystroke to the window the user was in before the flyout opened.
public interface IPasteBack
{
    // False where no keystroke can reach other apps (a Wayland session): the pick is only copied.
    bool CanPaste => true;

    Task IntoAsync(IntPtr targetWindow);
}
