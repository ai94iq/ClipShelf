namespace ClipShelf.App.Services;

// Applies light/dark/system theme and accent. Every window calls Attach once; a settings page calls
// Apply with the new settings (after SettingsService.Update), so ViewModels never touch the window.
public interface IThemeService
{
    void Attach(Window window);

    void Apply(AppSettings settings);
}
