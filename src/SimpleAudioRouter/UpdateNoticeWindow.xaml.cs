using System.Windows;

namespace SimpleAudioRouter;

public partial class UpdateNoticeWindow : Window
{
    private readonly string _releasePageUrl;

    public UpdateNoticeWindow(string latestVersion, string releasePageUrl)
    {
        InitializeComponent();
        _releasePageUrl = releasePageUrl;
        MessageText.Text = $"Version {latestVersion} is available on GitHub.";
    }

    private void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        AppLinks.Open(_releasePageUrl);
        DialogResult = true;
        Close();
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_CloseClick(object sender, System.Windows.RoutedEventArgs e)
    {
        e.Handled = true;
        DialogResult = false;
        Close();
    }
}
