using ClipShelf.Presentation.Hosting;

namespace ClipShelf.Presentation.Tests;

public sealed class SingleInstanceServiceTests
{
    private static string NewName() => "ClipShelf.Test." + Guid.NewGuid().ToString("N");

    [Fact]
    public void FirstClaimSucceeds()
    {
        using var service = new SingleInstanceService(NewName());
        Assert.True(service.TryClaim());
    }

    [Fact]
    public void SecondClaimFails()
    {
        var name = NewName();
        using var first = new SingleInstanceService(name);
        using var second = new SingleInstanceService(name);
        Assert.True(first.TryClaim());
        Assert.False(second.TryClaim());
    }

    [Fact]
    public async Task SentCommandIsReceived()
    {
        var name = NewName();
        using var service = new SingleInstanceService(name);
        Assert.True(service.TryClaim());

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.CommandReceived += (_, command) => received.TrySetResult(command);

        Assert.True(await SingleInstanceService.TrySendAsync(
            name, "--toggle", TestContext.Current.CancellationToken));
        var completed = await Task.WhenAny(
            received.Task, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(received.Task, completed);
        Assert.Equal("--toggle", await received.Task);
    }

    [Fact]
    public async Task SendRetriesUntilServerAppears()
    {
        var name = NewName();
        var send = SingleInstanceService.TrySendAsync(
            name, "--history", TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);

        using var service = new SingleInstanceService(name);
        Assert.True(service.TryClaim());
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.CommandReceived += (_, command) => received.TrySetResult(command);

        Assert.True(await send);
        var completed = await Task.WhenAny(
            received.Task, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(received.Task, completed);
        Assert.Equal("--history", await received.Task);
    }

    [Fact]
    public void ClaimSucceedsAgainAfterDispose()
    {
        var name = NewName();
        var first = new SingleInstanceService(name);
        Assert.True(first.TryClaim());
        first.Dispose();

        using var second = new SingleInstanceService(name);
        Assert.True(second.TryClaim());
    }

    [Fact]
    public async Task SendWithoutServerFails()
    {
        Assert.False(await SingleInstanceService.TrySendAsync(
            NewName(), "--toggle", TestContext.Current.CancellationToken));
    }
}
