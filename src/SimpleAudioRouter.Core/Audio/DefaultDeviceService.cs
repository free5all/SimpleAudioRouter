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
            PolicyConfigInterop.SetDefaultEndpoint(deviceId, ERole.Communications);
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

        return IsDefaultForRole(deviceId, Role.Multimedia)
            || IsDefaultForRole(deviceId, Role.Console)
            || IsDefaultForRole(deviceId, Role.Communications);
    }

    private bool IsDefaultForRole(string deviceId, Role role)
    {
        try
        {
            var currentId = _devices.GetDefaultPlaybackDeviceId(role);
            return string.Equals(currentId, deviceId, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
