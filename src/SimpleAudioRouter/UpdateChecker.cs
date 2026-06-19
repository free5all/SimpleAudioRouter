using System.Net.Http;
using System.Text.Json;

namespace SimpleAudioRouter;

internal sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string? LatestVersion,
    string ReleasePageUrl);

internal static class UpdateChecker
{
    private static readonly HttpClient Http = CreateClient();

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var releasePage = RepositoryInfo.ReleasePageUrl;

        try
        {
            var apiUrl = $"https://api.github.com/repos/{RepositoryInfo.Owner}/{RepositoryInfo.Name}/releases/latest";
            using var response = await Http.GetAsync(apiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new UpdateCheckResult(false, null, releasePage);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var tagName = document.RootElement.GetProperty("tag_name").GetString();
            var latestVersionText = NormalizeVersion(tagName);

            if (!Version.TryParse(AppInfo.Version, out var current)
                || !Version.TryParse(latestVersionText, out var latest))
            {
                return new UpdateCheckResult(false, latestVersionText, releasePage);
            }

            return new UpdateCheckResult(latest > current, latestVersionText, releasePage);
        }
        catch
        {
            return new UpdateCheckResult(false, null, releasePage);
        }
    }

    public static void OpenReleasePage(string? url = null) => AppLinks.Open(url ?? RepositoryInfo.ReleasePageUrl);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SimpleAudioRouter");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static string NormalizeVersion(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return "0.0.0";

        return tagName.Trim().TrimStart('v', 'V');
    }
}
