using ClipShelf.Core.Export;

namespace ClipShelf.Presentation.Services;

// Saves the whole clipboard history to a file and reports how many clips were written.
public interface IClipExportService
{
    Task<int> ExportAsync(string path, ClipExportFormat format, CancellationToken ct);
}
