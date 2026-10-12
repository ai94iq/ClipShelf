using ClipShelf.Desktop.Controls;
using ClipShelf.Presentation.Features.ClipboardPanel;

namespace ClipShelf.Desktop.Tests;

public sealed class GroupedRowsTests
{
    private static string Describe(object row) => row switch
    {
        ClipGroup group => $"[{group.Title}]",
        ClipItemViewModel clip => clip.Text,
        _ => "?",
    };

    [Fact]
    public Task Pinned_comes_first_with_its_header_then_recent() => Ui.Run(async () =>
    {
        var kit = new ViewModelKit();
        kit.StoreClip("recent one");
        kit.StoreClip("pinned one", pinned: true);
        kit.StoreClip("recent two");
        var rows = new GroupedRows(kit.ViewModel.Groups);

        await kit.ViewModel.RefreshAsync();

        Assert.Equal(["[Pinned]", "pinned one", "[Recent]", "recent one", "recent two"], rows.Select(Describe));
    });

    [Fact]
    public Task Rows_follow_a_regroup() => Ui.Run(async () =>
    {
        var kit = new ViewModelKit();
        kit.StoreClip("one");
        kit.StoreClip("two");
        var rows = new GroupedRows(kit.ViewModel.Groups);
        await kit.ViewModel.RefreshAsync();
        Assert.Equal(["[Recent]", "one", "two"], rows.Select(Describe));

        kit.StoreClip("three", pinned: true);
        await kit.ViewModel.RefreshAsync();

        Assert.Equal(["[Pinned]", "three", "[Recent]", "one", "two"], rows.Select(Describe));
    });

    // Only the Recent group's contents change; the group list itself stays the same.
    [Fact]
    public Task Rows_follow_a_new_clip_in_an_existing_group() => Ui.Run(async () =>
    {
        var kit = new ViewModelKit();
        kit.StoreClip("one");
        var rows = new GroupedRows(kit.ViewModel.Groups);
        await kit.ViewModel.RefreshAsync();

        kit.StoreClip("two");
        await kit.ViewModel.RefreshAsync();

        Assert.Equal(["[Recent]", "one", "two"], rows.Select(Describe));
    });
}
