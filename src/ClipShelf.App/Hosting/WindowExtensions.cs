namespace ClipShelf.App.Hosting;

public static class WindowExtensions
{
    // Call in every window constructor, right after InitializeComponent().
    public static void ApplyCultureDirection(this Window window)
    {
        if (window.Content is not FrameworkElement root) return;
        root.FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        root.Language = CultureInfo.CurrentUICulture.Name;
    }
}
