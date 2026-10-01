namespace ClipShelf.App.Services;

// The window that currently has focus, so dialogs know which XamlRoot to use.
public sealed class WindowContext
{
    public Window? Active { get; set; }
}
