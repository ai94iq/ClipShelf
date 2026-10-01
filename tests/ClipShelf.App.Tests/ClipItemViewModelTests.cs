using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using NSubstitute;

namespace ClipShelf.App.Tests;

public sealed class ClipItemViewModelTests
{
    private readonly IDateFormatter _dates = Substitute.For<IDateFormatter>();

    public ClipItemViewModelTests() => _dates.FormatSince(Arg.Any<DateTimeOffset>()).Returns("now");

    [Fact]
    public void The_app_name_is_shown_capitalized()
    {
        var item = Create("chrome");

        Assert.Equal("Chrome · now", item.SourceText);
    }

    [Fact]
    public void A_clip_without_an_app_name_shows_only_the_time()
    {
        var item = Create(null);

        Assert.Equal("now", item.SourceText);
    }

    private ClipItemViewModel Create(string? appName)
    {
        var panel = new ClipboardPanelViewModel(
            Substitute.For<IClipRepository>(),
            Substitute.For<IClipboardWriter>(),
            _dates,
            NullLogger<ClipboardPanelViewModel>.Instance);
        var model = new ClipListItem(
            1, "text", appName, false, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), new PageCursor("k", 1));

        return new ClipItemViewModel(model, _dates, panel);
    }
}
