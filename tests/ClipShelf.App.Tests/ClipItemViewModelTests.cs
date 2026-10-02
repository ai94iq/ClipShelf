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

    [Fact]
    public void The_category_is_shown_before_the_app_name()
    {
        var item = Create("chrome", categoryId: 1, categoryName: "Work");

        Assert.Equal("Work · Chrome · now", item.SourceText);
    }

    [Fact]
    public void A_category_without_an_app_name_still_shows()
    {
        var item = Create(null, categoryId: 1, categoryName: "Work");

        Assert.Equal("Work · now", item.SourceText);
    }

    [Fact]
    public void An_image_clip_shows_a_thumbnail_instead_of_the_preview()
    {
        var item = Create("chrome", thumbnail: [1, 2, 3]);

        Assert.True(item.HasImage);
        Assert.False(item.ShowPreview);
        Assert.Equal(new byte[] { 1, 2, 3 }, item.ThumbnailBytes);
    }

    private ClipItemViewModel Create(
        string? appName, long? categoryId = null, string? categoryName = null, byte[]? thumbnail = null)
    {
        var panel = new ClipboardPanelViewModel(
            Substitute.For<IClipRepository>(),
            Substitute.For<ICategoryRepository>(),
            new FakeCategoryLocks(),
            Substitute.For<ILockPasswordService>(),
            Substitute.For<IClipboardWriter>(),
            _dates,
            NullLogger<ClipboardPanelViewModel>.Instance);
        var model = new ClipListItem(
            1, "text", appName, false, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            new PageCursor("k", 1), categoryId, categoryName, thumbnail);

        return new ClipItemViewModel(model, _dates, panel);
    }
}
