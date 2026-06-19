using System.Text.Json;
using SimpleAudioRouter.Core.Audio;

namespace SimpleAudioRouter.Core.Settings;

public sealed class AppSettings
{
    public string? LeftDeviceId { get; set; }
    public string? RightDeviceId { get; set; }
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool TrayNotificationShown { get; set; }
    public string? UpdateNotificationDismissedForVersion { get; set; }
    public string? SavedDefaultDeviceId { get; set; }
    public DeviceRouteGains LeftDeviceGains { get; set; } = DeviceRouteGains.StereoDefault();
    public DeviceRouteGains RightDeviceGains { get; set; } = DeviceRouteGains.StereoDefault();

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SimpleAudioRouter",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
