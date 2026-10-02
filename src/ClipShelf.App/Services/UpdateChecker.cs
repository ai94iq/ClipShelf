using System.Net.Http;
using System.Text.Json;

namespace ClipShelf.App.Services;

// Reads the newest release tag from the public GitHub releases API.
public sealed class UpdateChecker : IUpdateChecker
{
    private const string LatestReleaseApi = "https://api.github.com/repos/ai94iq/ClipShelf/releases/latest";

    // 15 seconds: the first request pays for DNS and TLS on a cold start.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    // The running build's version, e.g. "0.2.0".
    public static string CurrentVersion { get; } =
        typeof(UpdateChecker).Assembly.GetName().Version is { } version
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "0.0.0";

    public async Task<string?> NewerVersionAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
        request.Headers.UserAgent.ParseAdd("ClipShelf");
        using var response = await Http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var tag = document.RootElement.TryGetProperty("tag_name", out var value) ? value.GetString() : null;

        return IsNewer(tag, CurrentVersion) ? tag!.TrimStart('v', 'V') : null;
    }

    public static bool IsNewer(string? tag, string current)
    {
        if (tag is null) return false;

        var text = tag.TrimStart('v', 'V');
        return Version.TryParse(text, out var latest)
            && Version.TryParse(current, out var mine)
            && latest > mine;
    }
}
