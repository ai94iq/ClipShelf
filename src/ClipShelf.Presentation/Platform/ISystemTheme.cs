namespace ClipShelf.Presentation.Platform;

// The taskbar/system theme, which the notification area follows independently of the app theme.
public interface ISystemTheme
{
    bool TaskbarIsLight { get; }

    event EventHandler? Changed;
}
