using Avalonia.Controls;

namespace Copyloft.Desktop.Shell;

// Shown once, on the first run: explains the hotkey and that the app lives in the tray, or, on
// desktops without a tray or a global hotkey (Wayland), how to reach it instead.
public sealed partial class WelcomeWindow : Window
{
    public WelcomeWindow(SettingsService settings, bool hasTray = true, bool globalHotkey = true, bool explainAccessibility = false)
    {
        InitializeComponent();
        WindowSurface.Attach(this, "CopyloftCanvasBrush");
        FlowDirection = Culture.IsRtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
        HotkeyText.Text = globalHotkey
            ? Tr.Format("Welcome_Hotkey", Copyloft.Core.Input.HotkeyText.Display(settings.Current.Hotkey))
            : Tr.Get("Settings_HotkeyWayland");
        TrayText.Text = Tr.Get(hasTray ? "Welcome_Tray" : "Welcome_NoTray");
        AccessibilityText.Text = Tr.Get("Welcome_Accessibility");
        AccessibilityText.IsVisible = explainAccessibility;
        OkButton.Click += (_, _) => Close();
    }

    // Only the first run shows it; the flag is saved at once so a crash cannot show it twice.
    public static void ShowOnFirstRun(SettingsService settings, Func<WelcomeWindow> create)
    {
        if (settings.Current.WelcomeShown) return;

        create().Show();
        settings.Update(settings.Current with { WelcomeShown = true });
    }
}
