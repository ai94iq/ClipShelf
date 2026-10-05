using ClipShelf.Core.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace ClipShelf.App.Shell;

// Shown once, on the first run: explains the hotkey and that the app lives in the tray.
public sealed partial class WelcomeWindow : Window
{
    public WelcomeWindow(SettingsService settings, IThemeService theme)
    {
        InitializeComponent();
        Title = Tr.Get("App_Name");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        AppWindow.Resize(new SizeInt32(460, 360));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleArea);
        this.ApplyCultureDirection();
        Center();

        WelcomeHotkey = Tr.Format("Welcome_Hotkey", HotkeyText.Format(settings.Current.Hotkey));
        theme.Attach(this);
    }

    public string WelcomeHotkey { get; }

    private void OnOk(object sender, RoutedEventArgs e) => Close();

    private void Center()
    {
        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var size = AppWindow.Size;
        AppWindow.Move(new PointInt32(
            work.X + ((work.Width - size.Width) / 2),
            work.Y + ((work.Height - size.Height) / 2)));
    }
}
