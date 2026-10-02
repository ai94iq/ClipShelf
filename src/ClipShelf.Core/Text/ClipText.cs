using System.Security.Cryptography;

namespace ClipShelf.Core.Text;

// Rules shared by capture, storage and display. Stored text is never mutated; only the
// hash ignores surrounding whitespace and line-ending style so one copy isn't stored twice.
public static class ClipText
{
    // Very large pastes (for example a whole document) are ignored rather than bloat the database.
    public const int MaxLength = 100_000;

    public static bool IsUsable(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= MaxLength;

    public static string Hash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n").Trim())));

    // Image clips deduplicate on the bytes themselves.
    public static string HashBytes(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    // One flattened line for the list row; the full text is kept for pasting.
    public static string Preview(string text, int maxLength = 200)
    {
        var line = text.ReplaceLineEndings(" ").Trim();
        return line.Length <= maxLength ? line : line[..maxLength] + "…";
    }
}
