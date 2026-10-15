using System.Globalization;
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

    // Off Windows Process.SessionId is the POSIX session id, different for a terminal and a
    // launcher; a name built from it let every launch start its own instance.
    [Fact]
    public void Off_windows_the_name_is_per_user_only()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows scopes the name per session");

        Assert.Equal("ClipShelf.SingleInstance." + Environment.UserName, SingleInstanceService.DefaultName);
    }

    // Off Windows the pipe is a Unix socket another process can simply re-bind, so the claim is
    // the lock file; holding it here stands in for a running instance in another process.
    [Fact]
    public void ClaimFailsWhileAnotherProcessHoldsTheLock()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "the named pipe itself is the claim on Windows");
        var name = NewName();
        using var held = new FileStream(
            SingleInstanceService.LockPath(name), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var service = new SingleInstanceService(name);

        Assert.False(service.TryClaim());
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

    [Fact]
    public void DefaultName_is_scoped_to_the_user()
    {
        Assert.StartsWith("ClipShelf.SingleInstance.", SingleInstanceService.DefaultName, StringComparison.Ordinal);
        Assert.Contains(Environment.UserName, SingleInstanceService.DefaultName, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultName_is_scoped_to_the_session()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "only Windows sessions scope the name");
        var session = System.Diagnostics.Process.GetCurrentProcess().SessionId;
        Assert.EndsWith(
            "." + session.ToString(CultureInfo.InvariantCulture),
            SingleInstanceService.DefaultName,
            StringComparison.Ordinal);
    }
}
