using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Copyloft.Desktop.Tests.TestApp))]

namespace Copyloft.Desktop.Tests;

public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()                    // real image decoding and text, not stub drawing
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
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
