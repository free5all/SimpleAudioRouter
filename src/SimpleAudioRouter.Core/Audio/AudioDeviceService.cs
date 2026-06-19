using NAudio.CoreAudioApi;

namespace SimpleAudioRouter.Core.Audio;

public sealed class AudioDeviceService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();

    public IReadOnlyList<AudioDeviceInfo> GetPlaybackDevices(bool excludeVac = true)
    {
        var devices = new List<AudioDeviceInfo>();
        foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            var name = device.FriendlyName;
            if (excludeVac && name.Contains("Virtual Audio Cable", StringComparison.OrdinalIgnoreCase))
                continue;

            devices.Add(new AudioDeviceInfo(device.ID, name));
        }

        return devices.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public AudioDeviceInfo? TryGetDevice(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        try
        {
            var device = _enumerator.GetDevice(deviceId);
            if (device.State != DeviceState.Active)
                return null;

            return new AudioDeviceInfo(device.ID, device.FriendlyName);
        }
        catch
        {
            return null;
        }
    }

    public MMDevice? TryGetMmDevice(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        try
        {
            var device = _enumerator.GetDevice(deviceId);
            return device.State == DeviceState.Active ? device : null;
        }
        catch
        {
            return null;
        }
    }

    public AudioDeviceInfo? GetDefaultPlaybackDevice()
    {
        try
        {
            var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return new AudioDeviceInfo(device.ID, device.FriendlyName);
        }
        catch
        {
            return null;
        }
    }

    public string? GetDefaultPlaybackDeviceId(Role role)
    {
        try
        {
            return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, role).ID;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _enumerator.Dispose();
}
