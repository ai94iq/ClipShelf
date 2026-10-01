namespace ClipShelf.App.Services;

// Applies light/dark/system theme and accent. MainWindow calls Attach once; a settings page calls Apply
// with the new settings (after SettingsStore.Save), so ViewModels never touch the window.
public interface IThemeService
{
    void Attach(Window window);

    void Apply(AppSettings settings);
}
