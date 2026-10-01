using System.Text.Json;
using ClipShelf.App.Services;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Export;
using ClipShelf.Core.Models;
using NSubstitute;

namespace ClipShelf.App.Tests;

public sealed class ClipExportServiceTests : IDisposable
{
    private readonly IClipRepository _repository = Substitute.For<IClipRepository>();
    private readonly List<string> _paths = [];

    [Fact]
    public async Task Writes_each_page_of_the_history()
    {
        var path = TempPath(".json");
        _repository.GetRecentAsync(null, 2, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(3, "c", category: "Work"), Item(2, "b") });
        _repository.GetRecentAsync(new PageCursor("k2", 2), 2, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem> { Item(1, "a") });
        var service = new ClipExportService(_repository, pageSize: 2);

        var count = await service.ExportAsync(path, ClipExportFormat.Json, TestContext.Current.CancellationToken);

        Assert.Equal(3, count);
        using var document = JsonDocument.Parse(
            await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        var texts = document.RootElement.EnumerateArray()
            .Select(element => element.GetProperty("text").GetString());
        Assert.Equal("c,b,a", string.Join(",", texts));
        Assert.Equal("Work", document.RootElement[0].GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task An_empty_history_writes_an_empty_json_array()
    {
        var path = TempPath(".json");
        _repository.GetRecentAsync(null, 200, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());
        var service = new ClipExportService(_repository);

        var count = await service.ExportAsync(path, ClipExportFormat.Json, TestContext.Current.CancellationToken);

        Assert.Equal(0, count);
        Assert.Equal("[]", (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).Trim());
    }

    [Fact]
    public async Task Csv_files_start_with_a_utf8_bom()
    {
        var path = TempPath(".csv");
        _repository.GetRecentAsync(null, 200, Arg.Any<CancellationToken>())
            .Returns(new List<ClipListItem>());
        var service = new ClipExportService(_repository);

        await service.ExportAsync(path, ClipExportFormat.Csv, TestContext.Current.CancellationToken);

        var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }

    public void Dispose()
    {
        foreach (var path in _paths.Where(File.Exists)) File.Delete(path);
    }

    private string TempPath(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"clipshelf-export-{Guid.NewGuid():N}{extension}");
        _paths.Add(path);
        return path;
    }

    private static ClipListItem Item(long id, string text, bool pinned = false, string? category = null) =>
        new(id, text, "Notepad", pinned,
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddSeconds(id),
            new PageCursor($"k{id}", id), category is null ? null : 1, category);
}
