using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SimpleAudioRouter;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {AppInfo.Version}";
        LogoImage.Source = LoadLogo();
    }

    private static BitmapImage LoadLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "SimpleAudioRouter.png");
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void GitHubLink_Click(object sender, RoutedEventArgs e) => AppLinks.Open(AppLinks.GitHubRepo);

    private void ReleasesLink_Click(object sender, RoutedEventArgs e) => AppLinks.Open(AppLinks.GitHubReleases);

    private void DiscordLink_Click(object sender, RoutedEventArgs e) => AppLinks.Open(AppLinks.Discord);

    private void CreatorLink_Click(object sender, RoutedEventArgs e) => AppLinks.Open(AppLinks.CreatorGitHub);

    private void VacLink_Click(object sender, RoutedEventArgs e) => AppLinks.Open(AppLinks.VacProductPage);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
