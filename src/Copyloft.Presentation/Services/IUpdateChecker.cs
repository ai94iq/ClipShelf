namespace Copyloft.Presentation.Services;

// Asks GitHub whether a newer Copyloft has been released.
public interface IUpdateChecker
{
    // The newest release version when it is newer than this build; null when up to date.
    // Throws when the check itself fails (offline, rate limit, ...).
    Task<string?> NewerVersionAsync(CancellationToken ct);
}
