using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Services;
using NSubstitute;

namespace ClipShelf.Core.Tests;

public sealed class ClipCaptureServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly ClipCaptureService _service;

    public ClipCaptureServiceTests() => _service = new ClipCaptureService(_repository);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_text_is_not_stored(string? text)
    {
        var stored = await _service.CaptureAsync(
            text, "Notepad", Now, keepUnpinned: 100, TestContext.Current.CancellationToken);

        Assert.False(stored);
        await _repository.DidNotReceiveWithAnyArgs().AddOrBumpAsync(
            default!, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Usable_text_is_stored_then_pruned()
    {
        var stored = await _service.CaptureAsync(
            "hello", "Notepad", Now, keepUnpinned: 50, TestContext.Current.CancellationToken);

        Assert.True(stored);
        await _repository.Received(1).AddOrBumpAsync("hello", "Notepad", Now, Arg.Any<CancellationToken>());
        await _repository.Received(1).PruneAsync(50, Arg.Any<CancellationToken>());
    }
}
