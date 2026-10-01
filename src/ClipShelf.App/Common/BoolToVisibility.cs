namespace ClipShelf.App.Common;

// For x:Bind function bindings, which cannot convert bool to Visibility the way WPF does.
public static class BoolToVisibility
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Collapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
}
