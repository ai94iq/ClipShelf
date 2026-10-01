namespace ClipShelf.App.Tests;

public sealed class PageViewModelTests
{
    [Fact]
    public async Task Failed_load_shows_error_state()
    {
        var page = new FakePage(_ => throw new InvalidOperationException("boom"));
        await page.EnsureLoadedAsync();
        Assert.Equal(LoadState.Error, page.State);
        Assert.False(string.IsNullOrEmpty(page.ErrorMessage));
    }

    [Fact]
    public async Task Nothing_to_show_gives_empty_state()
    {
        var page = new FakePage(_ => Task.FromResult(false));
        await page.EnsureLoadedAsync();
        Assert.Equal(LoadState.Empty, page.State);
    }

    private sealed class FakePage(Func<CancellationToken, Task<bool>> load) : PageViewModel(NullLogger.Instance)
    {
        protected override Task<bool> LoadCoreAsync(CancellationToken ct) => load(ct);
    }
}
