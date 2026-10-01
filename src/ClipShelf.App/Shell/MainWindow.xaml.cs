using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Shell;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel, IThemeService theme, AppLifetime lifetime)
    {
        InitializeComponent();
        Root.DataContext = viewModel;
        Title = Tr.Get("App_Name");
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.ApplyCultureDirection();

        // Mica exists only on Windows 11; Windows 10 gets the solid theme background.
        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop();
        else
            Root.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];

        theme.Attach(this);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(720, 560));

        // Closing the settings window only hides it; the app keeps running in the tray.
        AppWindow.Closing += (_, args) =>
        {
            if (lifetime.IsExiting) return;
            args.Cancel = true;
            AppWindow.Hide();
        };
    }
}
