using Avalonia.Headless;
using ClipShelf.Desktop;

namespace ClipShelf.Desktop.Tests;

// One headless Avalonia session per UI test: windows, bindings and layouts run without a display.
public sealed class HeadlessApp : IDisposable
{
    private readonly HeadlessUnitTestSession _session;

    public HeadlessApp() => _session = HeadlessUnitTestSession.StartNew(typeof(Program));

    public void Dispatch(Action action) =>
        _session.Dispatch(action, TestContext.Current.CancellationToken);

    public void Dispose() => _session.Dispose();
}
