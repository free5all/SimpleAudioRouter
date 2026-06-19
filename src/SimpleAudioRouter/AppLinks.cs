using System.Diagnostics;
using System.Windows;

namespace SimpleAudioRouter;

internal static class AppLinks
{
    public static string GitHubRepo => $"https://github.com/{RepositoryInfo.Owner}/{RepositoryInfo.Name}";

    public static string GitHubReleases => RepositoryInfo.ReleasePageUrl;

    public static string Discord => "https://discord.gg/q7zpdE9t";

    public static string CreatorGitHub => "https://github.com/free5all";

    public static string CreatorHandle => "@free5all";

    public static string VacProductPage => "https://vac.muzychenko.net/en/download.htm";

    public static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not open:\n{url}\n\n{ex.Message}",
                AppInfo.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
