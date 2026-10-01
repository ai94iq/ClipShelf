using ClipShelf.App.Services;

namespace ClipShelf.App.Tests.Settings;

public sealed class FakeStartupRegistration : IStartupRegistration
{
    public List<bool> Applied { get; } = [];

    public void Apply(bool enabled) => Applied.Add(enabled);
}
