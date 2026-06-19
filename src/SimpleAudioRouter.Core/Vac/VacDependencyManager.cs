using NAudio.CoreAudioApi;

namespace SimpleAudioRouter.Core.Vac;

public sealed class VacDependencyManager : IDisposable
{
    public const string VacLiteDownloadUrl = "https://software.muzychenko.net/freeware/vac470lite.zip";
    public const string VacProductPageUrl = "https://vac.muzychenko.net/en/download.htm";

    private readonly MMDeviceEnumerator _enumerator = new();

    public bool IsInstalled => TryGetEndpoints() is not null;

    public VacEndpoints? TryGetEndpoints(int cableNumber = 1)
    {
        MMDevice? capture = null;
        MMDevice? render = null;

        foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            if (MatchesVacLine(device.FriendlyName, cableNumber))
                capture = device;
        }

        foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (MatchesVacLine(device.FriendlyName, cableNumber))
                render = device;
        }

        if (capture is null || render is null)
            return null;

        return new VacEndpoints
        {
            CaptureDevice = capture,
            RenderDevice = render,
        };
    }

    private static bool MatchesVacLine(string friendlyName, int cableNumber)
    {
        if (!friendlyName.Contains("Virtual Audio Cable", StringComparison.OrdinalIgnoreCase))
            return false;

        return friendlyName.Contains($"Line {cableNumber}", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _enumerator.Dispose();
}
