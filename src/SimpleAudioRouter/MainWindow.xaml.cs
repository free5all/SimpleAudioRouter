using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SimpleAudioRouter.Core.Audio;
using SimpleAudioRouter.Core.Settings;
using SimpleAudioRouter.Core.Vac;
using SimpleAudioRouter.Controls;

namespace SimpleAudioRouter;

public partial class MainWindow : Window
{
    private readonly bool _launchInTray;
    private readonly CancellationTokenSource _pipeCts = new();

    private readonly AudioDeviceService _deviceService = new();
    private readonly DefaultDeviceService _defaultDeviceService;
    private readonly VacDependencyManager _vacManager = new();
    private readonly StereoChannelRouter _router;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _meterTimer;
    private TrayService? _tray;

    private bool _allowExit;
    private bool _initialized;
    private bool _suppressDeviceEvents;
    private string? _runningLeftId;
    private string? _runningRightId;
    private int _refreshTicks;
    private string? _lastRouterError;

    public MainWindow(bool launchInTray)
    {
        InitializeComponent();

        _launchInTray = launchInTray;
        _defaultDeviceService = new DefaultDeviceService(_deviceService);
        _router = new StereoChannelRouter(_deviceService, _defaultDeviceService, _vacManager);
        _settings = AppSettings.Load();

        Icon = AppIconHelper.GetAppImageSource();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) => RefreshUiAndRouting();

        _meterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _meterTimer.Tick += (_, _) => UpdateMeters();

        LeftRoutePanel.GainsChanged += (_, _) => OnGainsChanged();
        RightRoutePanel.GainsChanged += (_, _) => OnGainsChanged();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        InitializeApplication();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e) => EnsureInitialized();

    private void InitializeApplication()
    {
        if (_launchInTray)
            ShowInTaskbar = false;

        _tray = new TrayService(ShowFromTray, ShowSettingsFromTray, ExitApplication);

        if (!string.IsNullOrWhiteSpace(_settings.SavedDefaultDeviceId))
        {
            _defaultDeviceService.RestoreDefaultPlaybackDevice(_settings.SavedDefaultDeviceId);
            _settings.SavedDefaultDeviceId = null;
            _settings.Save();
        }

        if (_settings.StartWithWindows != StartupShortcutManager.IsEnabled())
            StartupShortcutManager.SetEnabled(_settings.StartWithWindows);

        SingleInstancePipe.StartServer(ShowFromTray, _pipeCts.Token);

        NormalizeDeviceSelections();
        LoadGainPanelsFromSettings();
        ApplyRouterGains();
        ApplyDeviceLists(_settings.LeftDeviceId, _settings.RightDeviceId);
        RefreshUiAndRouting();

        if (!_vacManager.IsInstalled)
            ShowVacSetup(attachToMain: !_launchInTray);
        else if (_launchInTray)
            HideToTray();

        _refreshTimer.Start();
        _meterTimer.Start();

        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await UpdateChecker.CheckAsync(_pipeCts.Token);
            if (!result.IsUpdateAvailable || result.LatestVersion is null)
                return;

            if (string.Equals(
                    _settings.UpdateNotificationDismissedForVersion,
                    result.LatestVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                var notice = new UpdateNoticeWindow(result.LatestVersion, result.ReleasePageUrl);
                if (IsVisible && Visibility == Visibility.Visible)
                {
                    notice.Owner = this;
                    notice.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    notice.ShowInTaskbar = false;
                }

                notice.ShowDialog();
                _settings.UpdateNotificationDismissedForVersion = result.LatestVersion;
                _settings.Save();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // Ignore update check failures.
        }
    }

    private void NormalizeDeviceSelections()
    {
        if (string.Equals(_settings.LeftDeviceId, _settings.RightDeviceId, StringComparison.OrdinalIgnoreCase))
            _settings.RightDeviceId = null;
    }

    private void LoadGainPanelsFromSettings()
    {
        LeftRoutePanel.SetGains(_settings.LeftDeviceGains);
        RightRoutePanel.SetGains(_settings.RightDeviceGains);
    }

    private void OnGainsChanged()
    {
        _settings.LeftDeviceGains = LeftRoutePanel.GetGains();
        _settings.RightDeviceGains = RightRoutePanel.GetGains();
        _settings.Save();
        ApplyRouterGains();
        _lastRouterError = null;
    }

    private void ApplyRouterGains() =>
        _router.SetGains(_settings.LeftDeviceGains, _settings.RightDeviceGains);

    private void UpdateMeters()
    {
        var levels = _router.Levels.ReadAndDecay();
        InputMeterLeft.Value = ToMeter(levels.InputLeft);
        InputMeterRight.Value = ToMeter(levels.InputRight);
        LeftRoutePanel.SetOutputLevels(levels.LeftOutputLeft, levels.LeftOutputRight);
        RightRoutePanel.SetOutputLevels(levels.RightOutputLeft, levels.RightOutputRight);
    }

    private static double ToMeter(float peak) => Math.Clamp(peak * 100f, 0f, 100f);

    private void ApplyDeviceLists(string? leftId, string? rightId)
    {
        _suppressDeviceEvents = true;
        try
        {
            var devices = _deviceService.GetPlaybackDevices();
            LeftDeviceComboBox.ItemsSource = FilterDevices(devices, rightId);
            RightDeviceComboBox.ItemsSource = FilterDevices(devices, leftId);
            SetComboSelection(LeftDeviceComboBox, leftId, devices);
            SetComboSelection(RightDeviceComboBox, rightId, devices);
        }
        finally
        {
            _suppressDeviceEvents = false;
        }
    }

    private static List<AudioDeviceInfo> FilterDevices(IReadOnlyList<AudioDeviceInfo> devices, string? excludeDeviceId)
    {
        if (string.IsNullOrWhiteSpace(excludeDeviceId))
            return devices.ToList();

        return devices
            .Where(d => !string.Equals(d.Id, excludeDeviceId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static void SetComboSelection(
        System.Windows.Controls.ComboBox combo,
        string? deviceId,
        IReadOnlyList<AudioDeviceInfo> allDevices)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            combo.SelectedIndex = -1;
            return;
        }

        if (combo.ItemsSource is not IEnumerable<AudioDeviceInfo> devices)
            return;

        var list = devices.ToList();
        var index = list.FindIndex(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            combo.SelectedIndex = index;
            return;
        }

        var known = allDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, known ?? new AudioDeviceInfo(deviceId, $"{deviceId} (unavailable)"));
        combo.ItemsSource = list;
        combo.SelectedIndex = 0;
    }

    private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressDeviceEvents)
            return;

        if (sender is not System.Windows.Controls.ComboBox changedCombo)
            return;

        var changedId = GetSelectedDeviceId(changedCombo);
        if (changedId is null)
            return;

        if (ReferenceEquals(changedCombo, LeftDeviceComboBox))
            _settings.LeftDeviceId = changedId;
        else
            _settings.RightDeviceId = changedId;

        if (_settings.LeftDeviceId is not null
            && _settings.RightDeviceId is not null
            && string.Equals(_settings.LeftDeviceId, _settings.RightDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            if (ReferenceEquals(changedCombo, LeftDeviceComboBox))
                _settings.RightDeviceId = null;
            else
                _settings.LeftDeviceId = null;
        }

        ApplyDeviceLists(_settings.LeftDeviceId, _settings.RightDeviceId);
        _settings.Save();
        _lastRouterError = null;
        RefreshUiAndRouting();
    }

    private static string? GetSelectedDeviceId(System.Windows.Controls.ComboBox combo) =>
        combo.SelectedItem is AudioDeviceInfo info ? info.Id : null;

    private void RefreshUiAndRouting()
    {
        _refreshTicks++;
        if (_refreshTicks % 5 == 0)
            ApplyDeviceLists(_settings.LeftDeviceId, _settings.RightDeviceId);

        ApplyRouterGains();

        var vacReady = _vacManager.IsInstalled;
        var leftId = _settings.LeftDeviceId;
        var rightId = _settings.RightDeviceId;

        var leftValid = leftId is not null && _deviceService.TryGetDevice(leftId) is not null;
        var rightValid = rightId is not null && _deviceService.TryGetDevice(rightId) is not null;
        var devicesDistinct = leftId is not null
            && rightId is not null
            && !string.Equals(leftId, rightId, StringComparison.OrdinalIgnoreCase);

        SetComboError(LeftDeviceComboBox, leftId is not null && !leftValid);
        SetComboError(RightDeviceComboBox, rightId is not null && !rightValid);
        UpdateTestButtons(leftValid && _router.IsRunning, rightValid && _router.IsRunning);

        if (!vacReady)
        {
            SetStatus("No driver", StatusIndicatorState.Error);
            StopRouterQuietly();
            return;
        }

        if (leftId is null || rightId is null)
        {
            SetStatus("Pick devices", StatusIndicatorState.Warning);
            StopRouterQuietly();
            return;
        }

        if (!devicesDistinct)
        {
            SetStatus("Pick devices", StatusIndicatorState.Warning);
            StopRouterQuietly();
            return;
        }

        if (!leftValid || !rightValid)
        {
            SetStatus("Device missing", StatusIndicatorState.Error);
            StopRouterQuietly();
            return;
        }

        try
        {
            var configChanged = !string.Equals(_runningLeftId, leftId, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_runningRightId, rightId, StringComparison.OrdinalIgnoreCase);

            if (_router.IsRunning && !configChanged)
            {
                UpdateRoutingStatus();
                return;
            }

            if (!configChanged && _lastRouterError is not null)
            {
                SetStatus("Error", StatusIndicatorState.Error);
                return;
            }

            StopRouterQuietly();
            _router.Start(leftId, rightId);
            _runningLeftId = leftId;
            _runningRightId = rightId;
            _lastRouterError = null;
            _settings.SavedDefaultDeviceId = _router.SavedDefaultDeviceId;
            _settings.Save();
            UpdateRoutingStatus();
        }
        catch (Exception ex)
        {
            _lastRouterError = ex.Message;
            SetStatus("Error", StatusIndicatorState.Error);
            StopRouterQuietly();
        }
    }

    private void UpdateRoutingStatus()
    {
        var vacId = _vacManager.TryGetEndpoints()?.RenderDevice.ID;
        var defaultOk = vacId is not null && _defaultDeviceService.IsDefaultPlaybackDevice(vacId);

        SetStatus(
            "Routing",
            defaultOk ? StatusIndicatorState.Ok : StatusIndicatorState.Warning);
    }

    private void SetStatus(string text, StatusIndicatorState indicator = StatusIndicatorState.Idle)
    {
        StatusText.Text = text;
        StatusIndicator.Fill = indicator switch
        {
            StatusIndicatorState.Ok => (System.Windows.Media.Brush)FindResource("StatusOkBrush"),
            StatusIndicatorState.Warning => (System.Windows.Media.Brush)FindResource("StatusWarnBrush"),
            StatusIndicatorState.Error => (System.Windows.Media.Brush)FindResource("StatusErrorBrush"),
            _ => (System.Windows.Media.Brush)FindResource("StatusIdleBrush"),
        };

        var detail = BuildStatusDetail(text, indicator);
        StatusBarPanel.ToolTip = detail;
        StatusText.ToolTip = detail;

        _tray?.SetStatus(text, MapTrayState(text, indicator));
    }

    private string BuildStatusDetail(string text, StatusIndicatorState indicator) => text switch
    {
        "Routing" when indicator == StatusIndicatorState.Ok =>
            "Routing audio. Left input goes to the left device, right input to the right device. Default playback is set to the virtual cable.",
        "Routing" =>
            "Routing audio, but Windows default playback could not be switched to the virtual cable. Set it manually in Sound settings, or run as administrator.",
        "Pick devices" =>
            "Select a different output device for left and right. The same device cannot be used for both.",
        "No driver" =>
            "Virtual audio cable driver is not installed. Open Driver to set it up.",
        "Device missing" =>
            "A selected output device is unplugged or unavailable. Pick another device.",
        "Not routing" =>
            "Routing is stopped. Select two devices to start again.",
        "Error" when !string.IsNullOrWhiteSpace(_lastRouterError) =>
            _lastRouterError!,
        "Error" =>
            "Routing failed. Check devices and try again.",
        "Idle" =>
            "Waiting to start.",
        _ => text,
    };

    private static TrayIconState MapTrayState(string text, StatusIndicatorState indicator = StatusIndicatorState.Idle) => text switch
    {
        "Routing" => TrayIconState.Routing,
        "Error" or "Device missing" or "No driver" => TrayIconState.Error,
        "Pick devices" or "Not routing" => TrayIconState.Warning,
        _ when indicator == StatusIndicatorState.Error => TrayIconState.Error,
        _ when indicator == StatusIndicatorState.Warning => TrayIconState.Warning,
        _ => TrayIconState.Idle,
    };

    private void StopRouterQuietly()
    {
        _router.Stop();
        _runningLeftId = null;
        _runningRightId = null;
        _settings.SavedDefaultDeviceId = null;
        _settings.Save();
    }

    private void UpdateTestButtons(bool leftValid, bool rightValid)
    {
        TestLeftDeviceButton.IsEnabled = leftValid;
        TestRightDeviceButton.IsEnabled = rightValid;
    }

    private static void SetComboError(System.Windows.Controls.ComboBox combo, bool hasError)
    {
        combo.BorderBrush = hasError
            ? System.Windows.Media.Brushes.IndianRed
            : (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("BorderBrush");
    }

    private void ShowVacSetup(bool attachToMain = true)
    {
        if (attachToMain && (!IsVisible || Visibility != Visibility.Visible))
            ShowFromTray();

        var wizard = new VacSetupWindow(_vacManager);
        if (attachToMain && IsVisible)
            wizard.Owner = this;
        else
            wizard.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        wizard.ShowDialog();
        RefreshUiAndRouting();

        if (_launchInTray && _vacManager.IsInstalled)
            HideToTray();
    }

    private void ShowSettingsFromTray() => ShowSettings(fromTray: true);

    private void ShowSettings(bool fromTray = false)
    {
        var previousMinimizeToTray = _settings.MinimizeToTrayOnClose;
        var dialog = new SettingsWindow(
            _settings.StartWithWindows,
            _settings.StartMinimized,
            _settings.MinimizeToTrayOnClose);

        if (fromTray && (!IsVisible || Visibility != Visibility.Visible))
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowInTaskbar = true;
        }
        else
        {
            dialog.Owner = this;
        }

        if (dialog.ShowDialog() != true)
            return;

        var minimizeToTrayChanged = previousMinimizeToTray != dialog.MinimizeToTrayOnClose;
        _settings.StartWithWindows = dialog.StartWithWindows;
        _settings.StartMinimized = dialog.StartMinimized;
        _settings.MinimizeToTrayOnClose = dialog.MinimizeToTrayOnClose;

        if (minimizeToTrayChanged && _settings.TrayNotificationShown)
            _settings.TrayNotificationShown = false;

        _settings.Save();
        StartupShortcutManager.SetEnabled(_settings.StartWithWindows);
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;

        if (!_settings.TrayNotificationShown)
        {
            _tray?.ShowTrayNotificationOnce();
            _settings.TrayNotificationShown = true;
            _settings.Save();
        }
    }

    private void ShowFromTray()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ShowFromTray);
            return;
        }

        if (Visibility != Visibility.Visible)
            Show();

        Visibility = Visibility.Visible;
        ShowInTaskbar = true;
        Opacity = 1;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowExit || e.Cancel)
            return;

        if (!_settings.MinimizeToTrayOnClose)
        {
            ExitApplication();
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _refreshTimer.Stop();
        _meterTimer.Stop();
        _pipeCts.Cancel();
        StopRouterQuietly();

        if (!string.IsNullOrWhiteSpace(_settings.SavedDefaultDeviceId))
        {
            _defaultDeviceService.RestoreDefaultPlaybackDevice(_settings.SavedDefaultDeviceId);
            _settings.SavedDefaultDeviceId = null;
            _settings.Save();
        }

        _tray?.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e) => ShowSettings();

    private void SetupVacMenuItem_Click(object sender, RoutedEventArgs e) => ShowVacSetup();

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutWindow { Owner = this };
        dialog.ShowDialog();
    }

    private async void TestLeftDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
            await PlayDeviceTestAsync(RouteTestChannel.Left, button);
    }

    private async void TestRightDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
            await PlayDeviceTestAsync(RouteTestChannel.Right, button);
    }

    private async Task PlayDeviceTestAsync(RouteTestChannel channel, System.Windows.Controls.Button button)
    {
        if (!_router.IsRunning)
        {
            SetStatus("Not routing", StatusIndicatorState.Warning);
            return;
        }

        if (!_vacManager.IsInstalled)
        {
            SetStatus("No driver", StatusIndicatorState.Error);
            return;
        }

        button.IsEnabled = false;
        try
        {
            await DeviceOutputTester.PlayRouteTestAsync(_vacManager, channel);
        }
        catch
        {
            SetStatus("Error", StatusIndicatorState.Error);
        }
        finally
        {
            button.IsEnabled = _router.IsRunning;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _pipeCts.Cancel();
        _router.Dispose();
        _vacManager.Dispose();
        _deviceService.Dispose();
        _tray?.Dispose();
        base.OnClosed(e);
    }
}
