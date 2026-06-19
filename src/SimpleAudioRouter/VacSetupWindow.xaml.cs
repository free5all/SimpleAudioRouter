using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using SimpleAudioRouter.Core.Vac;

namespace SimpleAudioRouter;

public partial class VacSetupWindow : Window
{
    private readonly VacDependencyManager _vac;
    private readonly DispatcherTimer _pollTimer;
    private string? _extractedSetupPath;
    private CancellationTokenSource? _downloadCts;
    private int _pollAttempts;

    public VacSetupWindow(VacDependencyManager vac)
    {
        InitializeComponent();
        _vac = vac;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += PollTimer_Tick;

        Loaded += (_, _) => TryUseCachedSetup();
    }

    private void TryUseCachedSetup()
    {
        var cached = VacPackageDownloader.FindCachedSetupExe();
        if (cached is null)
            return;

        _extractedSetupPath = VacSetupExeResolver.ResolveFromExistingPath(cached);
        if (_extractedSetupPath is null)
            return;

        StatusText.Text = "Previous download found. Click Install.";
        InstallButton.IsEnabled = true;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        SetBusy(true, "Downloading...");
        try
        {
            _extractedSetupPath = await VacPackageDownloader.DownloadAndExtractAsync(token);
            StatusText.Text = "Download complete. Click Install.";
            InstallButton.IsEnabled = true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Download failed: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var setupPath = VacSetupExeResolver.ResolveFromExistingPath(_extractedSetupPath);
        if (setupPath is null || !File.Exists(setupPath))
        {
            StatusText.Text = "Download first.";
            return;
        }

        _extractedSetupPath = setupPath;

        try
        {
            Process.Start(new ProcessStartInfo(setupPath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(setupPath)!,
            });

            StatusText.Text = "Waiting for driver...";
            _pollAttempts = 0;
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Install failed: {ex.Message}";
        }
    }

    private void PollTimer_Tick(object? sender, EventArgs e)
    {
        _pollAttempts++;
        if (_vac.IsInstalled)
        {
            _pollTimer.Stop();
            StatusText.Text = "Driver detected.";
            DialogResult = true;
            Close();
            return;
        }

        if (_pollAttempts >= 60)
        {
            _pollTimer.Stop();
            StatusText.Text = "Not detected yet. Finish install or reboot.";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void LinkText_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo(VacDependencyManager.VacProductPageUrl) { UseShellExecute = true });
    }

    private void SetBusy(bool busy, string? status = null)
    {
        DownloadButton.IsEnabled = !busy;
        BusyBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (status is not null)
            StatusText.Text = status;
    }

    protected override void OnClosed(EventArgs e)
    {
        _pollTimer.Stop();
        _downloadCts?.Cancel();
        base.OnClosed(e);
    }
}
