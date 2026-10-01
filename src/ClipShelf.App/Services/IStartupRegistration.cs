namespace ClipShelf.App.Services;

// Keeps the per-user startup entry in step with the setting.
public interface IStartupRegistration
{
    void Apply(bool enabled);
}
