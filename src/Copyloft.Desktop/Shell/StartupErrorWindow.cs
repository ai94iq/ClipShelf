using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Copyloft.Desktop.Platform;

namespace Copyloft.Desktop.Shell;

// Shown when startup fails, so the app does not just vanish (seen on macOS after a denied
// Keychain request). Closing it ends the app.
public sealed class StartupErrorWindow : Window
{
    public StartupErrorWindow(Exception error)
    {
        Title = Tr.Get("App_Name");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        WindowSurface.Attach(this, "CopyloftCanvasBrush");

        var ok = new Button { Content = Tr.Get("Common_Ok"), HorizontalAlignment = HorizontalAlignment.Right };
        ok.Classes.Add("accent");
        ok.Click += (_, _) => Close();
        Content = new StackPanel
        {
            Margin = new Thickness(28),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = Message(error), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = Tr.Format("Startup_Log", AppPaths.Logs), TextWrapping = TextWrapping.Wrap },
                ok,
            },
        };
    }

    internal static string Message(Exception error)
    {
        for (var e = error; e is not null; e = e.InnerException)
            if (e is KeyringUnavailableException) return Tr.Get("Startup_KeyringDenied");
        return Tr.Get("Startup_Failed");
    }
}
