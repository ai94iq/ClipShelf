namespace Copyloft.Presentation.Services;

// Applies light/dark/system theme and accent. The head implements this; view models call Apply.
public interface IThemeService
{
    void Apply(AppSettings settings);
}
