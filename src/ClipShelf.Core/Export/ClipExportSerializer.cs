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

    // A cell that starts with one of these can be read as a formula by spreadsheet apps.
    private static readonly char[] FormulaLeaders = ['=', '+', '-', '@', '\t', '\r'];

    public static string Serialize(IReadOnlyList<ClipExportRow> rows, ClipExportFormat format) =>
        format == ClipExportFormat.Csv ? ToCsv(rows) : JsonSerializer.Serialize(rows, JsonOptions);

    private static string ToCsv(IReadOnlyList<ClipExportRow> rows)
    {
        var builder = new StringBuilder("text,app_name,category,pinned,copied_at");
        foreach (var row in rows)
        {
            builder.Append("\r\n")
                .Append(Escape(row.Text)).Append(',')
                .Append(Escape(row.AppName ?? string.Empty)).Append(',')
                .Append(Escape(row.CategoryName ?? string.Empty)).Append(',')
                .Append(row.Pinned ? "true" : "false").Append(',')
                .Append(row.CopiedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    // RFC 4180 quoting, plus the OWASP mitigation for formula-leading text: a leading apostrophe
    // makes spreadsheets treat the cell as text (they hide the mark); JSON exports stay untouched.
    private static string Escape(string value)
    {
        if (value.Length > 0 && FormulaLeaders.Contains(value[0])) value = "'" + value;
        return value.IndexOfAny(CsvSpecials) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
