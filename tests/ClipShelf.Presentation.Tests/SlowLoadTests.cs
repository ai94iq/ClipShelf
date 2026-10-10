using ClipShelf.Presentation.Features.ClipboardPanel;
using ClipShelf.Presentation.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ClipShelf.Presentation.Tests;

// A load that never returns must say so in the log instead of spinning silently.
public sealed class SlowLoadTests
{
    [Fact]
    public async Task HungLoad_LogsASlowWarning()
    {
        var repository = Substitute.For<IClipRepository>();
        repository.GetRecentAsync(
                Arg.Any<PageCursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>(),
                Arg.Any<long?>(), Arg.Any<IReadOnlyCollection<long>?>())
            .Returns(new TaskCompletionSource<IReadOnlyList<ClipListItem>>().Task);
        var log = Substitute.For<ILogger<ClipboardPanelViewModel>>();
        var vm = new ClipboardPanelViewModel(
            repository,
            Substitute.For<ICategoryRepository>(),
            new FakeCategoryLocks(),
            Substitute.For<ILockPasswordService>(),
            Substitute.For<IClipboardWriter>(),
            Substitute.For<IDateFormatter>(),
            log)
        {
            SlowLoadThreshold = TimeSpan.FromMilliseconds(50),
        };

        _ = vm.RefreshQuietAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        log.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("exceeded")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
