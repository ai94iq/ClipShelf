using Copyloft.Presentation.Services;

namespace Copyloft.Presentation.Tests;

public sealed class FakeStartupRegistration : IStartupRegistration
{
    public List<bool> Applied { get; } = [];

    public void Apply(bool enabled) => Applied.Add(enabled);
}
