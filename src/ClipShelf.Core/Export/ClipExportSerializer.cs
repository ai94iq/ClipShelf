using System.Text.Json;

namespace ClipShelf.Core.Export;

// Turns export rows into the file text for the chosen format.
public static class ClipExportSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly char[] CsvSpecials = [',', '"', '\r', '\n'];

    public static string Serialize(IReadOnlyList<ClipExportRow> rows, ClipExportFormat format) =>
        format == ClipExportFormat.Csv ? ToCsv(rows) : JsonSerializer.Serialize(rows, JsonOptions);

    private static string ToCsv(IReadOnlyList<ClipExportRow> rows)
    {
        var builder = new StringBuilder("text,app_name,pinned,copied_at");
        foreach (var row in rows)
        {
            builder.Append("\r\n")
                .Append(Escape(row.Text)).Append(',')
                .Append(Escape(row.AppName ?? string.Empty)).Append(',')
                .Append(row.Pinned ? "true" : "false").Append(',')
                .Append(row.CopiedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    // RFC 4180: quote a field that holds a comma, a quote or a line break, and double its quotes.
    private static string Escape(string value) =>
        value.IndexOfAny(CsvSpecials) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
}
