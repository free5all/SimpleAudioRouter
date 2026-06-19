using NAudio.CoreAudioApi;

namespace SimpleAudioRouter.Core.Vac;

public sealed class VacEndpoints
{
    public required MMDevice CaptureDevice { get; init; }
    public required MMDevice RenderDevice { get; init; }

    public string CaptureName => CaptureDevice.FriendlyName;
    public string RenderName => RenderDevice.FriendlyName;
}
