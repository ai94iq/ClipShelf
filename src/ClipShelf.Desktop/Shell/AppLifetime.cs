namespace ClipShelf.Desktop.Shell;

// Shared flag: closing a window hides it, unless the app is really exiting.
public sealed class AppLifetime
{
    public bool IsExiting { get; set; }
}
