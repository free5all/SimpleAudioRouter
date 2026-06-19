using NAudio.CoreAudioApi;
using SimpleAudioRouter.Core.Native;

namespace SimpleAudioRouter.Core.Audio;

public sealed class DefaultDeviceService
{
    private readonly AudioDeviceService _devices;

    public DefaultDeviceService(AudioDeviceService devices)
    {
        _devices = devices;
    }

    public string? CaptureCurrentDefaultId()
    {
        return _devices.GetDefaultPlaybackDevice()?.Id;
    }

    public bool TrySetDefaultPlaybackDevice(string deviceId)
    {
        if (IsDefaultPlaybackDevice(deviceId))
            return true;

        try
        {
            PolicyConfigInterop.SetDefaultEndpoint(deviceId, ERole.Console);
            PolicyConfigInterop.SetDefaultEndpoint(deviceId, ERole.Multimedia);
            return true;
        }
        catch
        {
            return IsDefaultPlaybackDevice(deviceId);
        }
    }

    public bool IsDefaultPlaybackDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        // Multimedia is the role Windows exposes as "Default device" for playback.
        return string.Equals(
            _devices.GetDefaultPlaybackDeviceId(Role.Multimedia),
            deviceId,
            StringComparison.OrdinalIgnoreCase);
    }

    public void RestoreDefaultPlaybackDevice(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        if (_devices.TryGetMmDevice(deviceId) is null)
            return;

        TrySetDefaultPlaybackDevice(deviceId);
    }
}
