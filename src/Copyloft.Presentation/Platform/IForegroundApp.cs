namespace Copyloft.Presentation.Platform;

// The process name of the window that was in front when the copy happened.
public interface IForegroundApp
{
    string? Name();
}
