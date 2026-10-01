namespace ClipShelf.Core.Export;

// One exported clip. Text is the full clip text, not the preview.
public sealed record ClipExportRow(
    string Text, string? AppName, bool Pinned, DateTimeOffset CopiedAtUtc, string? CategoryName = null);
