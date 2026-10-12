using Avalonia.Threading;
using ClipShelf.Desktop.Platform;

namespace ClipShelf.Desktop.Tests;

public sealed class UiDispatcherTests
{
    [Fact]
    public Task Work_posted_from_a_pool_thread_runs_on_the_ui_thread() => Ui.Run(async () =>
    {
        var dispatcher = new AvaloniaUiDispatcher();
        var ranOnUi = new TaskCompletionSource<bool>();

        await Task.Run(() => dispatcher.Post(() => ranOnUi.SetResult(Dispatcher.UIThread.CheckAccess())));

        Assert.True(await ranOnUi.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    });
}
