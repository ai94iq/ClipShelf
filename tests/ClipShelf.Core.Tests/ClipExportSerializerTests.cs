using System.Text.Json;
using ClipShelf.Core.Export;

namespace ClipShelf.Core.Tests;

public sealed class ClipExportSerializerTests
{
    [Fact]
    public void Json_rows_use_camel_case_field_names()
    {
        var json = ClipExportSerializer.Serialize([Row("hello")], ClipExportFormat.Json);

        using var document = JsonDocument.Parse(json);
        var item = document.RootElement[0];
        Assert.Equal("hello", item.GetProperty("text").GetString());
        Assert.Equal("Notepad", item.GetProperty("appName").GetString());
        Assert.False(item.GetProperty("pinned").GetBoolean());
        Assert.Equal("2026-01-02T03:04:05+00:00", item.GetProperty("copiedAtUtc").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("categoryName").ValueKind);
    }

    [Fact]
    public void Json_includes_the_category_name()
    {
        var json = ClipExportSerializer.Serialize([Row("hello", category: "Work")], ClipExportFormat.Json);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("Work", document.RootElement[0].GetProperty("categoryName").GetString());
    }

    [Fact]
    public void Json_keeps_line_breaks_and_unicode()
    {
        var json = ClipExportSerializer.Serialize([Row("line1\nline2 ✓")], ClipExportFormat.Json);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("line1\nline2 ✓", document.RootElement[0].GetProperty("text").GetString());
    }

    [Fact]
    public void Csv_lists_a_header_and_one_row_per_clip()
    {
        var csv = ClipExportSerializer.Serialize(
            [Row("one"), Row("two", pinned: true, category: "Work")], ClipExportFormat.Csv);

        var lines = csv.Split("\r\n");
        Assert.Equal("text,app_name,category,pinned,copied_at", lines[0]);
        Assert.Equal("one,Notepad,,false,2026-01-02T03:04:05.0000000+00:00", lines[1]);
        Assert.Equal("two,Notepad,Work,true,2026-01-02T03:04:05.0000000+00:00", lines[2]);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public void Csv_quotes_commas_quotes_and_line_breaks()
    {
        var csv = ClipExportSerializer.Serialize(
            [Row("a,b"), Row("say \"hi\""), Row("line1\nline2")], ClipExportFormat.Csv);

        Assert.Contains("\"a,b\"", csv);
        Assert.Contains("\"say \"\"hi\"\"\"", csv);
        Assert.Contains("\"line1\nline2\"", csv);
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(A1)")]
    [InlineData("-2+3")]
    [InlineData("- bullet")]
    [InlineData("@cmd")]
    public void Csv_neutralizes_text_that_looks_like_a_formula(string text)
    {
        var csv = ClipExportSerializer.Serialize([Row(text)], ClipExportFormat.Csv);

        var line = csv.Split("\r\n")[1];
        Assert.StartsWith($"'{text}", line);
    }

    [Fact]
    public void Csv_leaves_normal_text_alone()
    {
        var csv = ClipExportSerializer.Serialize([Row("plain text")], ClipExportFormat.Csv);

        var line = csv.Split("\r\n")[1];
        Assert.StartsWith("plain text,", line);
    }

    private static ClipExportRow Row(string text, bool pinned = false, string? category = null) =>
        new(text, "Notepad", pinned, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), category);
}
