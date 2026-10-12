using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(ClipShelf.Desktop.Tests.TestApp))]

namespace ClipShelf.Desktop.Tests;

public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

// Runs test bodies on the headless Avalonia UI thread; one app per test assembly.
internal static class Ui
{
    private static HeadlessUnitTestSession Session =>
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Ui).Assembly);

    public static Task Run(Action body) =>
        Session.Dispatch(body, TestContext.Current.CancellationToken);

    public static Task Run(Func<Task> body) =>
        Session.Dispatch(async () => { await body(); return 0; }, TestContext.Current.CancellationToken);
}
