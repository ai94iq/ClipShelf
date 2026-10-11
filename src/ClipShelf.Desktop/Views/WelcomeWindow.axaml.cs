using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Views;

// Shown once, on the first run: explains the hotkey and that the app lives in the tray.
public sealed partial class WelcomeWindow : Window
{
    // Parameterless constructor for XAML designer/loader
    public WelcomeWindow()
    {
        WelcomeHotkey = string.Empty;
        InitializeComponent();
        Title = Tr.Get("App_Name");
        ApplyCulture();
        SetAppIcon();
        Center();
    }

    public WelcomeWindow(SettingsService settings)
    {
        WelcomeHotkey = Tr.Format("Welcome_Hotkey", HotkeyText.Format(settings.Current.Hotkey));
        InitializeComponent();
        Title = Tr.Get("App_Name");
        ApplyCulture();
        SetAppIcon();
        Center();
    }

    public string WelcomeHotkey { get; private set; }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();

    private void ApplyCulture()
    {
        FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        if (Culture.IsRtl)
            FontFamily = new FontFamily("avares://ClipShelf/Assets/Fonts/NotoSansArabic-Regular.ttf#Noto Sans Arabic");
    }

    private void SetAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(path)) Icon = new WindowIcon(path);
    }

    // 460x360 centered over the primary working area, mirroring the WinUI first-run window.
    private void Center()
    {
        if (Screens.Primary is not { } primary) return;
        var area = primary.WorkingArea;
        Position = new PixelPoint(
            area.X + ((area.Width - (int)Width) / 2),
            area.Y + ((area.Height - (int)Height) / 2));
    }
}
