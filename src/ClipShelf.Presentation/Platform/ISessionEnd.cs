namespace ClipShelf.Presentation.Platform;

// Raised when the OS is ending the session (sign-out or shutdown).
public interface ISessionEnd
{
    event EventHandler? Ending;
}
