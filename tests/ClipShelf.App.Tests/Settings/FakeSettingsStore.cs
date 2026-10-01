using ClipShelf.App.Hosting;

namespace ClipShelf.App.Tests.Settings;

// In-memory settings store so tests never touch %LOCALAPPDATA%.
public sealed class FakeSettingsStore : ISettingsStore
{
    public AppSettings? Saved { get; private set; }

    public AppSettings Load() => new();

    public void Save(AppSettings settings) => Saved = settings;
}
