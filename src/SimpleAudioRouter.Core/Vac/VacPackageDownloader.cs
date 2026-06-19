using System.IO.Compression;
using System.Net.Http;

namespace SimpleAudioRouter.Core.Vac;

public static class VacPackageDownloader
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static string GetDownloadRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "SimpleAudioRouter", "VacDownload");
        Directory.CreateDirectory(root);
        return root;
    }

    public static string? FindCachedSetupExe()
    {
        var root = GetDownloadRoot();
        if (!Directory.Exists(root))
            return null;

        string? newest = null;
        var newestTime = DateTime.MinValue;

        foreach (var workDir in Directory.EnumerateDirectories(root))
        {
            var setup = VacSetupExeResolver.FindSetupExe(workDir);
            if (setup is null)
                continue;

            var writeTime = File.GetLastWriteTimeUtc(setup);
            if (writeTime <= newestTime)
                continue;

            newestTime = writeTime;
            newest = setup;
        }

        return newest is not null && File.Exists(newest) ? newest : null;
    }

    public static async Task<string> DownloadAndExtractAsync(CancellationToken cancellationToken = default)
    {
        var workDir = Path.Combine(GetDownloadRoot(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var zipPath = Path.Combine(workDir, "vac470lite.zip");
        var extractDir = Path.Combine(workDir, "extracted");

        try
        {
            await DownloadToFileAsync(VacDependencyManager.VacLiteDownloadUrl, zipPath, cancellationToken)
                .ConfigureAwait(false);

            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            var setupPath = VacSetupExeResolver.FindSetupExe(extractDir);

            if (setupPath is null)
            {
                TryDeleteDirectory(workDir);
                throw new InvalidOperationException("Download finished but setup.exe was not found in the package.");
            }

            return setupPath;
        }
        catch
        {
            TryDeleteDirectory(workDir);
            throw;
        }
    }

    private static async Task DownloadToFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await HttpClient
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var networkStream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await networkStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SimpleAudioRouter/1.0");
        return client;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; locked files may remain until reboot.
        }
    }
}
